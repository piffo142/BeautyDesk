using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace SIG.BeautyDesk.Api.Services;

public sealed class TwilioSmsGateway(HttpClient httpClient, IOptions<TwilioSmsOptions> options)
{
    public async Task SendAsync(string toNumber, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toNumber))
        {
            throw new InvalidOperationException("Destination number is required.");
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AccountSid) ||
            string.IsNullOrWhiteSpace(settings.AuthToken) ||
            string.IsNullOrWhiteSpace(settings.FromNumber))
        {
            throw new InvalidOperationException("Twilio SMS is not configured.");
        }

        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{settings.AccountSid}/Messages.json";
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.AccountSid}:{settings.AuthToken}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = toNumber,
            ["From"] = settings.FromNumber,
            ["Body"] = message
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Twilio SMS send failed with status {(int)response.StatusCode}: {details}");
        }
    }
}
