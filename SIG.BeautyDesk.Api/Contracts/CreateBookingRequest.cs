using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class CreateBookingRequest
{
    public Guid? EnquiryId { get; init; }

    public Guid CustomerId { get; init; }

    public Guid ServiceId { get; init; }

    public Guid StaffId { get; init; }

    public Guid ResourceId { get; init; }

    public BookingStatus? RequestedStatus { get; init; }

    public bool DepositRequired { get; init; }

    public bool DepositPaid { get; init; }

    public string? DepositTakenVia { get; init; }

    public string? InboundCallSid { get; init; }

    public string? RecordingUrl { get; init; }

    public string? TranscriptText { get; init; }

    public string? N8nWorkflowExecutionId { get; init; }

    public int? CallDurationSec { get; init; }

    public required IReadOnlyList<CreateBookingSegmentRequest> Segments { get; init; }
}

public sealed class CreateBookingSegmentRequest
{
    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public bool StaffOccupied { get; init; }

    public bool ResourceOccupied { get; init; }
}
