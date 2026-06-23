using System.Net.Http.Json;
using SIG.BeautyDesk.Maui.Models;

namespace SIG.BeautyDesk.Maui.Services;

public sealed class BeautyDeskApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<AvailabilitySlotModel>> GetAvailabilityAsync(
        Guid serviceId,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            serviceId,
            rangeStartUtc,
            rangeEndUtc,
            slotStepMinutes = 15,
            maxResults = 150
        };

        using var response = await httpClient.PostAsJsonAsync("/api/contracts/GetAvailability", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GetAvailabilityResponse>(cancellationToken);
        return payload?.Slots ?? [];
    }

    public async Task<string> CreateBookingAsync(CreateBookingDraft draft, CancellationToken cancellationToken)
    {
        var request = new
        {
            customerId = draft.CustomerId,
            serviceId = draft.ServiceId,
            staffId = draft.StaffId,
            resourceId = draft.ResourceId,
            depositRequired = false,
            depositPaid = false,
            segments = new[]
            {
                new
                {
                    startUtc = draft.StartUtc,
                    endUtc = draft.EndUtc,
                    staffOccupied = true,
                    resourceOccupied = true
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync("/api/contracts/CreateBooking", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreateBookingResponse>(cancellationToken);
        return payload?.BookingId.ToString() ?? throw new InvalidOperationException("Missing booking id.");
    }

    private sealed class GetAvailabilityResponse
    {
        public required List<AvailabilitySlotModel> Slots { get; init; }
    }

    private sealed class CreateBookingResponse
    {
        public required Guid BookingId { get; init; }
    }
}
