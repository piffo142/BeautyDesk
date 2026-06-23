namespace SIG.BeautyDesk.Core.Entities;

public sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public bool ConsentMarketing { get; set; }

    public bool ConsentSMS { get; set; }

    public Guid? PreferredTherapistId { get; set; }

    public DateTime? PatchTestExpiry { get; set; }

    public Staff? PreferredTherapist { get; set; }

    public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
