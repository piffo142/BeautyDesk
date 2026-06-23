using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Core.Entities;

public sealed class Enquiry
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public EnquiryChannel Channel { get; set; }

    public EnquiryStatus Status { get; set; }

    public string? InboundCallSid { get; set; }

    public string? RecordingUrl { get; set; }

    public string? AssignedToUserId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string? Tags { get; set; }

    public string? TranscriptText { get; set; }

    public DateTime? EscalatedUtc { get; set; }

    public string? EscalationReason { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
