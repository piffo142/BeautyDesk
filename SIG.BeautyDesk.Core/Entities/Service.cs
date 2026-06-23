namespace SIG.BeautyDesk.Core.Entities;

public sealed class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public decimal Price { get; set; }

    public bool RequiresPatchTest { get; set; }

    public int BufferBeforeMin { get; set; }

    public int BufferAfterMin { get; set; }

    public string? RequiredSkillTag { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
