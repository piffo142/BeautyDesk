namespace SIG.BeautyDesk.Api.Contracts;

public sealed class N8nVoiceIntentRequest
{
    public required string Intent { get; init; }

    public GetAvailabilityRequest? Availability { get; init; }

    public IdentifyOrCreateCustomerRequest? Customer { get; init; }

    public CreateBookingRequest? Booking { get; init; }

    public EscalateToHumanRequest? Escalation { get; init; }
}

public sealed class N8nVoiceIntentResponse
{
    public required string Intent { get; init; }

    public required object Payload { get; init; }
}
