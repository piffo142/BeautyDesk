using Microsoft.AspNetCore.SignalR.Client;

namespace SIG.BeautyDesk.Maui.Services;

public sealed class ReceptionRealtimeClient
{
    private readonly HubConnection _connection;

    public ReceptionRealtimeClient()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/reception")
            .WithAutomaticReconnect()
            .Build();
    }

    public event Action<string>? EscalationReceived;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection.On<object>("EnquiryEscalated", payload =>
        {
            EscalationReceived?.Invoke($"Escalated enquiry received: {payload}");
        });

        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(cancellationToken);
        }
    }
}
