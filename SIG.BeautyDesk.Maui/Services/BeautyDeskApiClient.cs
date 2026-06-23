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

    public async Task<IReadOnlyList<StaffAgendaBookingModel>> GetStaffAgendaAsync(
        Guid staffId,
        DateTime dayUtc,
        CancellationToken cancellationToken)
    {
        var dayText = Uri.EscapeDataString(dayUtc.ToString("O"));
        using var response = await httpClient.GetAsync($"/api/staff/{staffId}/agenda?dayUtc={dayText}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<StaffAgendaResponse>(cancellationToken);
        return payload?.Bookings ?? [];
    }

    public async Task UpdateBookingStatusAsync(Guid bookingId, string status, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PatchAsJsonAsync(
            $"/api/bookings/{bookingId}/status",
            new { status },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RegisterPushDeviceAsync(Guid staffId, int platform, string pushToken, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/push/register",
            new { staffId, platform, pushToken },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed class GetAvailabilityResponse
    {
        public required List<AvailabilitySlotModel> Slots { get; init; }
    }

    private sealed class CreateBookingResponse
    {
        public required Guid BookingId { get; init; }
    }

    private sealed class StaffAgendaResponse
    {
        public required List<StaffAgendaBookingModel> Bookings { get; init; }
    }
}
