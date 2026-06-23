using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class CreateEnquiryRequest
{
    public Guid CustomerId { get; init; }

    public EnquiryChannel Channel { get; init; }

    public string? InboundCallSid { get; init; }

    public string? RecordingUrl { get; init; }

    public string? AssignedToUserId { get; init; }

    public string? Tags { get; init; }

    public string? TranscriptText { get; init; }
}
