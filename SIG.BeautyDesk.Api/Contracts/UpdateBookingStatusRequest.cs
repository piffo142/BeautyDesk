using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; init; }
}
