using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class EnquiryResponse
{
    public required Guid EnquiryId { get; init; }

    public required Guid CustomerId { get; init; }

    public required EnquiryChannel Channel { get; init; }

    public required EnquiryStatus Status { get; init; }

    public required DateTime CreatedUtc { get; init; }

    public DateTime? EscalatedUtc { get; init; }

    public string? EscalationReason { get; init; }
}
