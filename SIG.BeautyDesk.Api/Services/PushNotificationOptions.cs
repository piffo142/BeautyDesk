namespace SIG.BeautyDesk.Api.Services;

public sealed class PushNotificationOptions
{
    public const string SectionName = "PushNotifications";

    public bool Enabled { get; init; } = true;

    public string ApnsBundleId { get; init; } = string.Empty;

    public string FcmProjectId { get; init; } = string.Empty;
}
