namespace SIG.BeautyDesk.Api.Contracts;

public sealed class EscalateToHumanRequest
{
    public Guid EnquiryId { get; init; }

    public required string Reason { get; init; }
}
