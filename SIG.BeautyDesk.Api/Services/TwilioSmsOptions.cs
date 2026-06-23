namespace SIG.BeautyDesk.Api.Services;

public sealed class TwilioSmsOptions
{
    public const string SectionName = "TwilioSms";

    public string AccountSid { get; init; } = string.Empty;

    public string AuthToken { get; init; } = string.Empty;

    public string FromNumber { get; init; } = string.Empty;
}
