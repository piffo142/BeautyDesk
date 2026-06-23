namespace SIG.BeautyDesk.Api.Security;

public sealed class N8nAuthOptions
{
    public const string SectionName = "N8nAuth";

    public string ApiKey { get; init; } = string.Empty;

    public string SigningSecret { get; init; } = string.Empty;

    public int ClockSkewSeconds { get; init; } = 300;
}
