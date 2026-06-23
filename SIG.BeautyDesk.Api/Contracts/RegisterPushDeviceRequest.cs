using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Api.Contracts;

public sealed class RegisterPushDeviceRequest
{
    public Guid StaffId { get; init; }

    public DevicePlatform Platform { get; init; }

    public required string PushToken { get; init; }
}
