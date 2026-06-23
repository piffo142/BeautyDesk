namespace SIG.BeautyDesk.Core.Entities;

public sealed class Staff
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string WorkingHoursJson { get; set; } = "{}";

    public string SkillTags { get; set; } = "[]";

    public int MaxConcurrentBookings { get; set; } = 1;

    public ICollection<Customer> PreferredByCustomers { get; set; } = new List<Customer>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<StaffDevice> Devices { get; set; } = new List<StaffDevice>();
}
