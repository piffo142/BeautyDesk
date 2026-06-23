using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Core.Entities;

public sealed class Booking
{
    public Guid Id { get; set; }

    public Guid? EnquiryId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid StaffId { get; set; }

    public Guid ResourceId { get; set; }

    public BookingStatus Status { get; set; }

    public bool DepositRequired { get; set; }

    public bool DepositPaid { get; set; }

    public string? DepositTakenVia { get; set; }

    public string? RemindersSent { get; set; }

    public Enquiry? Enquiry { get; set; }

    public Customer Customer { get; set; } = null!;

    public Service Service { get; set; } = null!;

    public Staff Staff { get; set; } = null!;

    public Resource Resource { get; set; } = null!;

    public ICollection<BookingSegment> Segments { get; set; } = new List<BookingSegment>();
}
