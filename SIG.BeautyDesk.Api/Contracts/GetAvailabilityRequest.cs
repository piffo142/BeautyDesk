namespace SIG.BeautyDesk.Api.Contracts;

public sealed class GetAvailabilityRequest
{
    public Guid ServiceId { get; init; }

    public DateTime RangeStartUtc { get; init; }

    public DateTime RangeEndUtc { get; init; }

    public Guid? PreferredStaffId { get; init; }

    public Guid? PreferredResourceId { get; init; }

    public int SlotStepMinutes { get; init; } = 15;

    public int MaxResults { get; init; } = 200;
}
