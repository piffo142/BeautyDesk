using SIG.BeautyDesk.Api.Contracts;

namespace SIG.BeautyDesk.Api.Services;

public sealed class N8nVoiceOrchestrationService(
    BookingEngineService bookingEngineService,
    CustomerService customerService,
    EnquiryService enquiryService)
{
    public async Task<N8nVoiceIntentResponse> HandleIntentAsync(
        N8nVoiceIntentRequest request,
        CancellationToken cancellationToken)
    {
        var intent = request.Intent.Trim().ToLowerInvariant();
        return intent switch
        {
            "get_availability" => new N8nVoiceIntentResponse
            {
                Intent = request.Intent,
                Payload = await bookingEngineService.GetAvailabilityAsync(
                    request.Availability ?? throw new InvalidOperationException("Availability payload is required."),
                    cancellationToken)
            },
            "identify_or_create_customer" => new N8nVoiceIntentResponse
            {
                Intent = request.Intent,
                Payload = await customerService.IdentifyOrCreateAsync(
                    request.Customer ?? throw new InvalidOperationException("Customer payload is required."),
                    cancellationToken)
            },
            "create_booking" => new N8nVoiceIntentResponse
            {
                Intent = request.Intent,
                Payload = await bookingEngineService.CreateBookingAsync(
                    request.Booking ?? throw new InvalidOperationException("Booking payload is required."),
                    cancellationToken)
            },
            "escalate_to_human" => await HandleEscalationAsync(request, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported voice intent '{request.Intent}'.")
        };
    }

    private async Task<N8nVoiceIntentResponse> HandleEscalationAsync(
        N8nVoiceIntentRequest request,
        CancellationToken cancellationToken)
    {
        var escalation = request.Escalation ?? throw new InvalidOperationException("Escalation payload is required.");
        var payload = await enquiryService.EscalateToHumanAsync(escalation.EnquiryId, escalation.Reason, cancellationToken);
        return new N8nVoiceIntentResponse
        {
            Intent = request.Intent,
            Payload = payload
        };
    }
}
