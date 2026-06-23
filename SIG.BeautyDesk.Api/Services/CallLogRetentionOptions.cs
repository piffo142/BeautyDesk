namespace SIG.BeautyDesk.Api.Services;

public sealed class CallLogRetentionOptions
{
    public const string SectionName = "CallLogRetention";

    public int RetentionDays { get; init; } = 30;
}
