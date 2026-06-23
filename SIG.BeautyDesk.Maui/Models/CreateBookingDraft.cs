namespace SIG.BeautyDesk.Maui.Models;

public sealed class CreateBookingDraft
{
    public Guid CustomerId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid StaffId { get; set; }

    public Guid ResourceId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }
}
