using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class StaffAgendaResponse
{
    public required Guid StaffId { get; init; }

    public required DateTime DayUtc { get; init; }

    public required IReadOnlyList<StaffAgendaBookingItem> Bookings { get; init; }
}

public sealed class StaffAgendaBookingItem
{
    public required Guid BookingId { get; init; }

    public required Guid CustomerId { get; init; }

    public required Guid ServiceId { get; init; }

    public required Guid ResourceId { get; init; }

    public required BookingStatus Status { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }
}
