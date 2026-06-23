namespace SIG.BeautyDesk.Core.Entities;

public sealed class BookingSegment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public bool StaffOccupied { get; set; }

    public bool ResourceOccupied { get; set; }

    public Booking Booking { get; set; } = null!;
}
