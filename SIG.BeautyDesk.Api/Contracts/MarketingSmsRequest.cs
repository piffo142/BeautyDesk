namespace SIG.BeautyDesk.Api.Contracts;

public sealed class MarketingSmsRequest
{
    public Guid CustomerId { get; init; }

    public required string Message { get; init; }
}

public sealed class MarketingSmsResponse
{
    public bool Allowed { get; init; }

    public required string Result { get; init; }
}
