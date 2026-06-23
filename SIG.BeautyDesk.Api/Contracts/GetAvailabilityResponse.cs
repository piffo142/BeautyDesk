namespace SIG.BeautyDesk.Api.Contracts;

public sealed class GetAvailabilityResponse
{
    public required IReadOnlyList<AvailabilitySlotResponse> Slots { get; init; }
}

public sealed class AvailabilitySlotResponse
{
    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    public required Guid StaffId { get; init; }

    public required Guid ResourceId { get; init; }
}
