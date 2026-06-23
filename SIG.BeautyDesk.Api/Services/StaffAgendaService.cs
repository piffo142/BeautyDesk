using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Core.Enums;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class StaffAgendaService(BeautyDeskDbContext dbContext, PushNotificationService pushNotificationService)
{
    public async Task<StaffAgendaResponse> GetAgendaAsync(Guid staffId, DateTime dayUtc, CancellationToken cancellationToken)
    {
        if (dayUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("dayUtc must be in UTC.");
        }

        var dayStart = dayUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var rows = await (
                from booking in dbContext.Bookings.AsNoTracking()
                from segment in booking.Segments
                where booking.StaffId == staffId
                      && segment.StartUtc >= dayStart
                      && segment.StartUtc < dayEnd
                orderby segment.StartUtc
                select new
                {
                    booking.Id,
                    booking.CustomerId,
                    booking.ServiceId,
                    booking.ResourceId,
                    booking.Status,
                    segment.StartUtc,
                    segment.EndUtc
                })
            .ToListAsync(cancellationToken);

        return new StaffAgendaResponse
        {
            StaffId = staffId,
            DayUtc = dayStart,
            Bookings = rows.Select(x => new StaffAgendaBookingItem
            {
                BookingId = x.Id,
                CustomerId = x.CustomerId,
                ServiceId = x.ServiceId,
                ResourceId = x.ResourceId,
                Status = x.Status,
                StartUtc = x.StartUtc,
                EndUtc = x.EndUtc
            }).ToList()
        };
    }

    public async Task UpdateStatusAsync(Guid bookingId, BookingStatus newStatus, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings.SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        booking.Status = newStatus;
        await dbContext.SaveChangesAsync(cancellationToken);

        await pushNotificationService.NotifyStaffAsync(
            booking.StaffId,
            "Booking status updated",
            $"Booking {booking.Id} is now {newStatus}.",
            cancellationToken);
    }
}
