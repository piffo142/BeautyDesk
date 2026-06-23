namespace SIG.BeautyDesk.Maui.Models;

public sealed class StaffAgendaBookingModel
{
    public required Guid BookingId { get; init; }

    public required Guid CustomerId { get; init; }

    public required Guid ServiceId { get; init; }

    public required Guid ResourceId { get; init; }

    public required string Status { get; set; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    public string Display => $"{StartUtc:HH:mm} - {EndUtc:HH:mm} ({Status})";
}
