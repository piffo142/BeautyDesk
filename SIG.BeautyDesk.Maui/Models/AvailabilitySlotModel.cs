namespace SIG.BeautyDesk.Maui.Models;

public sealed class AvailabilitySlotModel
{
    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    public required Guid StaffId { get; init; }

    public required Guid ResourceId { get; init; }

    public string Display => $"{StartUtc:HH:mm} - {EndUtc:HH:mm} ({ResourceId.ToString()[..8]})";
}
