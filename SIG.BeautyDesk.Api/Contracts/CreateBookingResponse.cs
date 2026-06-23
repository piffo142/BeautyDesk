using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class CreateBookingResponse
{
    public required Guid BookingId { get; init; }

    public required BookingStatus Status { get; init; }

    public required string SmsConfirmationStatus { get; init; }

    public required IReadOnlyList<CreateBookingSegmentResponse> Segments { get; init; }
}

public sealed class CreateBookingSegmentResponse
{
    public required Guid SegmentId { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }

    public required bool StaffOccupied { get; init; }

    public required bool ResourceOccupied { get; init; }
}
