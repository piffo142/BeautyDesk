using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace SIG.BeautyDesk.Api.Security;

public sealed class N8nWebhookAuthMiddleware(RequestDelegate next, IOptions<N8nAuthOptions> options)
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string SignatureHeader = "X-Signature";
    private const string TimestampHeader = "X-Timestamp";

    private readonly N8nAuthOptions _options = options.Value;

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/n8n", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.SigningSecret))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "N8n webhook authentication is not configured." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) ||
            !string.Equals(apiKey.ToString(), _options.ApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(TimestampHeader, out var timestampHeader) ||
            !long.TryParse(timestampHeader, out var timestamp))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid timestamp." });
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > _options.ClockSkewSeconds)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Timestamp is outside accepted clock skew." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(SignatureHeader, out var signatureHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing signature." });
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var payload = $"{timestamp}.{body}";
        var secretBytes = Encoding.UTF8.GetBytes(_options.SigningSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(secretBytes);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();

        var receivedSignature = signatureHeader.ToString().Trim().ToLowerInvariant();
        var computedBytes = Encoding.UTF8.GetBytes(computedHex);
        var receivedBytes = Encoding.UTF8.GetBytes(receivedSignature);
        if (computedBytes.Length != receivedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid signature." });
            return;
        }

        await next(context);
    }
}
