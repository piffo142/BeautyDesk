using SIG.BeautyDesk.Core.Enums;

namespace SIG.BeautyDesk.Core.Entities;

public sealed class StaffDevice
{
    public Guid Id { get; set; }

    public Guid StaffId { get; set; }

    public DevicePlatform Platform { get; set; }

    public string PushToken { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public Staff Staff { get; set; } = null!;
}
