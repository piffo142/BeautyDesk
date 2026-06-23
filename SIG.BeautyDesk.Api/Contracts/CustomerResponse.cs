namespace SIG.BeautyDesk.Api.Contracts;

public sealed class CustomerResponse
{
    public required Guid CustomerId { get; init; }

    public required string Name { get; init; }

    public required string Phone { get; init; }

    public bool ConsentMarketing { get; init; }

    public bool ConsentSMS { get; init; }

    public DateTime? PatchTestExpiry { get; init; }
}
