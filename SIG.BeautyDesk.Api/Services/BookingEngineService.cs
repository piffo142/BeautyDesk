using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Core.Entities;
using SIG.BeautyDesk.Core.Enums;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class BookingEngineService(
    BeautyDeskDbContext dbContext,
    IOptions<CallLogRetentionOptions> retentionOptions)
{
    private static readonly BookingStatus[] ActiveStatuses =
    [
        BookingStatus.Tentative,
        BookingStatus.Confirmed,
        BookingStatus.Arrived
    ];

    public async Task<GetAvailabilityResponse> GetAvailabilityAsync(
        GetAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ValidateUtc(request.RangeStartUtc, nameof(request.RangeStartUtc));
        ValidateUtc(request.RangeEndUtc, nameof(request.RangeEndUtc));
        if (request.RangeEndUtc <= request.RangeStartUtc)
        {
            throw new InvalidOperationException("RangeEndUtc must be greater than RangeStartUtc.");
        }

        var service = await dbContext.Services
            .SingleOrDefaultAsync(x => x.Id == request.ServiceId, cancellationToken);
        if (service is null)
        {
            throw new InvalidOperationException($"Service '{request.ServiceId}' was not found.");
        }

        var allStaff = await dbContext.Staff.AsNoTracking().ToListAsync(cancellationToken);
        var eligibleStaff = allStaff
            .Where(x => request.PreferredStaffId is null || x.Id == request.PreferredStaffId)
            .Where(x => HasSkill(x, service.RequiredSkillTag))
            .ToList();

        var allResources = await dbContext.Resources.AsNoTracking().ToListAsync(cancellationToken);
        var eligibleResources = allResources
            .Where(x => request.PreferredResourceId is null || x.Id == request.PreferredResourceId)
            .ToList();

        var conflicts = await (
                from segment in dbContext.BookingSegments.AsNoTracking()
                join booking in dbContext.Bookings.AsNoTracking() on segment.BookingId equals booking.Id
                where ActiveStatuses.Contains(booking.Status)
                      && segment.StartUtc < request.RangeEndUtc
                      && segment.EndUtc > request.RangeStartUtc
                select new ConflictView(
                    booking.StaffId,
                    booking.ResourceId,
                    segment.StartUtc,
                    segment.EndUtc,
                    segment.StaffOccupied,
                    segment.ResourceOccupied))
            .ToListAsync(cancellationToken);

        var slotStep = request.SlotStepMinutes <= 0 ? 15 : request.SlotStepMinutes;
        var slotDuration = TimeSpan.FromMinutes(service.BufferBeforeMin + service.DurationMinutes + service.BufferAfterMin);
        var maxResults = request.MaxResults <= 0 ? 200 : request.MaxResults;
        var slots = new List<AvailabilitySlotResponse>(maxResults);

        for (var current = request.RangeStartUtc; current + slotDuration <= request.RangeEndUtc; current = current.AddMinutes(slotStep))
        {
            var slotEnd = current + slotDuration;
            foreach (var staff in eligibleStaff)
            {
                if (!IsWithinWorkingHours(staff.WorkingHoursJson, current, slotEnd))
                {
                    continue;
                }

                foreach (var resource in eligibleResources)
                {
                    var hasStaffConflict = conflicts.Any(x =>
                        x.StaffId == staff.Id &&
                        x.StaffOccupied &&
                        Overlaps(current, slotEnd, x.StartUtc, x.EndUtc));

                    if (hasStaffConflict)
                    {
                        continue;
                    }

                    var hasResourceConflict = conflicts.Any(x =>
                        x.ResourceId == resource.Id &&
                        x.ResourceOccupied &&
                        Overlaps(current, slotEnd, x.StartUtc, x.EndUtc));

                    if (hasResourceConflict)
                    {
                        continue;
                    }

                    slots.Add(new AvailabilitySlotResponse
                    {
                        StartUtc = current,
                        EndUtc = slotEnd,
                        StaffId = staff.Id,
                        ResourceId = resource.Id
                    });

                    if (slots.Count >= maxResults)
                    {
                        return new GetAvailabilityResponse { Slots = slots };
                    }
                }
            }
        }

        return new GetAvailabilityResponse { Slots = slots };
    }

    public async Task<CreateBookingResponse> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Segments.Count == 0)
        {
            throw new InvalidOperationException("At least one booking segment is required.");
        }

        foreach (var segment in request.Segments)
        {
            ValidateUtc(segment.StartUtc, nameof(segment.StartUtc));
            ValidateUtc(segment.EndUtc, nameof(segment.EndUtc));
            if (segment.EndUtc <= segment.StartUtc)
            {
                throw new InvalidOperationException("Each segment must have EndUtc greater than StartUtc.");
            }
        }

        var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer '{request.CustomerId}' was not found.");
        var service = await dbContext.Services.SingleOrDefaultAsync(x => x.Id == request.ServiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{request.ServiceId}' was not found.");
        _ = await dbContext.Staff.SingleOrDefaultAsync(x => x.Id == request.StaffId, cancellationToken)
            ?? throw new InvalidOperationException($"Staff '{request.StaffId}' was not found.");
        _ = await dbContext.Resources.SingleOrDefaultAsync(x => x.Id == request.ResourceId, cancellationToken)
            ?? throw new InvalidOperationException($"Resource '{request.ResourceId}' was not found.");

        Enquiry? enquiry = null;
        if (request.EnquiryId.HasValue)
        {
            enquiry = await dbContext.Enquiries.SingleOrDefaultAsync(x => x.Id == request.EnquiryId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Enquiry '{request.EnquiryId.Value}' was not found.");
        }

        var nowUtc = DateTime.UtcNow;
        var hasValidPatchTest = customer.PatchTestExpiry.HasValue && customer.PatchTestExpiry.Value >= nowUtc;
        var requestedStatus = request.RequestedStatus ?? BookingStatus.Confirmed;
        var finalStatus = service.RequiresPatchTest && !hasValidPatchTest
            ? BookingStatus.Tentative
            : requestedStatus;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        foreach (var segment in request.Segments)
        {
            const string conflictSql = """
                                       SELECT bs.*
                                       FROM BookingSegments bs WITH (UPDLOCK, HOLDLOCK)
                                       INNER JOIN Bookings b WITH (UPDLOCK, HOLDLOCK) ON b.Id = bs.BookingId
                                       WHERE b.Status IN ('Tentative', 'Confirmed', 'Arrived')
                                         AND ((b.StaffId = @staffId AND bs.StaffOccupied = 1)
                                           OR (b.ResourceId = @resourceId AND bs.ResourceOccupied = 1))
                                         AND bs.StartUtc < @segmentEndUtc
                                         AND bs.EndUtc > @segmentStartUtc
                                       """;

            var hasConflict = await dbContext.BookingSegments
                .FromSqlRaw(
                    conflictSql,
                    new SqlParameter("@staffId", request.StaffId),
                    new SqlParameter("@resourceId", request.ResourceId),
                    new SqlParameter("@segmentEndUtc", segment.EndUtc),
                    new SqlParameter("@segmentStartUtc", segment.StartUtc))
                .AsNoTracking()
                .AnyAsync(cancellationToken);

            if (hasConflict)
            {
                throw new InvalidOperationException("The requested slot is no longer available.");
            }
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EnquiryId = request.EnquiryId,
            CustomerId = request.CustomerId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            ResourceId = request.ResourceId,
            Status = finalStatus,
            CreatedUtc = nowUtc,
            DepositRequired = request.DepositRequired,
            DepositPaid = request.DepositPaid,
            DepositTakenVia = request.DepositTakenVia,
            InboundCallSid = request.InboundCallSid ?? enquiry?.InboundCallSid,
            RecordingUrl = request.RecordingUrl ?? enquiry?.RecordingUrl
        };

        foreach (var segment in request.Segments)
        {
            booking.Segments.Add(new BookingSegment
            {
                Id = Guid.NewGuid(),
                StartUtc = segment.StartUtc,
                EndUtc = segment.EndUtc,
                StaffOccupied = segment.StaffOccupied,
                ResourceOccupied = segment.ResourceOccupied
            });
        }

        dbContext.Bookings.Add(booking);

        if (enquiry is not null)
        {
            enquiry.Status = EnquiryStatus.Booked;
            enquiry.InboundCallSid = booking.InboundCallSid ?? enquiry.InboundCallSid;
            enquiry.RecordingUrl = booking.RecordingUrl ?? enquiry.RecordingUrl;
            if (!string.IsNullOrWhiteSpace(request.TranscriptText))
            {
                enquiry.TranscriptText = request.TranscriptText;
            }
        }

        if (!string.IsNullOrWhiteSpace(booking.InboundCallSid))
        {
            var retentionDays = retentionOptions.Value.RetentionDays <= 0 ? 30 : retentionOptions.Value.RetentionDays;
            dbContext.CallLogs.Add(new CallLog
            {
                Id = Guid.NewGuid(),
                CallSid = booking.InboundCallSid,
                FromNumber = customer.Phone,
                RecordingUrl = booking.RecordingUrl,
                DurationSec = request.CallDurationSec ?? 0,
                CreatedUtc = nowUtc,
                RetainUntilUtc = nowUtc.AddDays(retentionDays),
                N8nWorkflowExecutionId = request.N8nWorkflowExecutionId,
                RawTranscriptJson = request.TranscriptText
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateBookingResponse
        {
            BookingId = booking.Id,
            Status = booking.Status,
            Segments = booking.Segments
                .Select(x => new CreateBookingSegmentResponse
                {
                    SegmentId = x.Id,
                    StartUtc = x.StartUtc,
                    EndUtc = x.EndUtc,
                    StaffOccupied = x.StaffOccupied,
                    ResourceOccupied = x.ResourceOccupied
                })
                .ToList()
        };
    }

    private static bool HasSkill(Staff staff, string? requiredSkillTag)
    {
        if (string.IsNullOrWhiteSpace(requiredSkillTag))
        {
            return true;
        }

        var tags = JsonSerializer.Deserialize<List<string>>(staff.SkillTags) ?? [];
        return tags.Any(x => string.Equals(x, requiredSkillTag, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWithinWorkingHours(string workingHoursJson, DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Date != endUtc.Date)
        {
            return false;
        }

        var windowsByDay = JsonSerializer.Deserialize<Dictionary<string, List<WorkingHoursWindow>>>(workingHoursJson) ?? [];
        var day = startUtc.DayOfWeek.ToString();
        if (!windowsByDay.TryGetValue(day, out var windows) || windows.Count == 0)
        {
            return false;
        }

        var startTime = TimeOnly.FromDateTime(startUtc);
        var endTime = TimeOnly.FromDateTime(endUtc);
        return windows.Any(window =>
            TimeOnly.Parse(window.Start) <= startTime &&
            TimeOnly.Parse(window.End) >= endTime);
    }

    private static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB) =>
        startA < endB && endA > startB;

    private static void ValidateUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException($"{name} must be provided in UTC.");
        }
    }

    private sealed record ConflictView(
        Guid StaffId,
        Guid ResourceId,
        DateTime StartUtc,
        DateTime EndUtc,
        bool StaffOccupied,
        bool ResourceOccupied);

    private sealed class WorkingHoursWindow
    {
        public required string Start { get; init; }

        public required string End { get; init; }
    }
}
