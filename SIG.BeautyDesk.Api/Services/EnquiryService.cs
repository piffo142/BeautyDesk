using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SIG.BeautyDesk.Api.Contracts;
using SIG.BeautyDesk.Api.Hubs;
using SIG.BeautyDesk.Core.Entities;
using SIG.BeautyDesk.Core.Enums;
using SIG.BeautyDesk.Data;

namespace SIG.BeautyDesk.Api.Services;

public sealed class EnquiryService(BeautyDeskDbContext dbContext, IHubContext<ReceptionHub> receptionHub)
{
    public async Task<EnquiryResponse> CreateEnquiryAsync(CreateEnquiryRequest request, CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.Customers
            .AnyAsync(x => x.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException($"Customer '{request.CustomerId}' was not found.");
        }

        var enquiry = new Enquiry
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Channel = request.Channel,
            Status = EnquiryStatus.New,
            InboundCallSid = request.InboundCallSid,
            RecordingUrl = request.RecordingUrl,
            AssignedToUserId = request.AssignedToUserId,
            CreatedUtc = DateTime.UtcNow,
            Tags = request.Tags,
            TranscriptText = request.TranscriptText
        };

        dbContext.Enquiries.Add(enquiry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(enquiry);
    }

    public async Task<EnquiryResponse> EscalateToHumanAsync(Guid enquiryId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Escalation reason is required.");
        }

        var enquiry = await dbContext.Enquiries.SingleOrDefaultAsync(x => x.Id == enquiryId, cancellationToken);
        if (enquiry is null)
        {
            throw new InvalidOperationException($"Enquiry '{enquiryId}' was not found.");
        }

        enquiry.EscalatedUtc = DateTime.UtcNow;
        enquiry.EscalationReason = reason;
        enquiry.Status = EnquiryStatus.Triaged;

        await dbContext.SaveChangesAsync(cancellationToken);

        await receptionHub.Clients.All.SendAsync(
            "EnquiryEscalated",
            new
            {
                enquiryId = enquiry.Id,
                customerId = enquiry.CustomerId,
                reason,
                escalatedUtc = enquiry.EscalatedUtc
            },
            cancellationToken);

        return ToResponse(enquiry);
    }

    private static EnquiryResponse ToResponse(Enquiry enquiry) =>
        new()
        {
            EnquiryId = enquiry.Id,
            CustomerId = enquiry.CustomerId,
            Channel = enquiry.Channel,
            Status = enquiry.Status,
            CreatedUtc = enquiry.CreatedUtc,
            EscalatedUtc = enquiry.EscalatedUtc,
            EscalationReason = enquiry.EscalationReason
        };
}
