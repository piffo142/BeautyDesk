namespace SIG.BeautyDesk.Api.Contracts;

public sealed class IdentifyOrCreateCustomerRequest
{
    public required string Phone { get; init; }

    public required string Name { get; init; }

    public string? Email { get; init; }

    public string? Notes { get; init; }

    public bool ConsentMarketing { get; init; }

    public bool ConsentSMS { get; init; }

    public DateTime? PatchTestExpiry { get; init; }
}
