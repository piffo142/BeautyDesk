namespace SIG.BeautyDesk.Core.Entities;

public sealed class Resource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
