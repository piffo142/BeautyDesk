using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIG.BeautyDesk.Maui.Models;
using SIG.BeautyDesk.Maui.Services;

namespace SIG.BeautyDesk.Maui.ViewModels;

public partial class ReceptionViewModel : ObservableObject
{
    private readonly BeautyDeskApiClient _apiClient;
    private readonly ReceptionRealtimeClient _realtimeClient;

    public ReceptionViewModel(BeautyDeskApiClient apiClient, ReceptionRealtimeClient realtimeClient)
    {
        _apiClient = apiClient;
        _realtimeClient = realtimeClient;
        _realtimeClient.EscalationReceived += message => StatusMessage = message;
    }

    public ObservableCollection<AvailabilitySlotModel> Slots { get; } = [];

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _customerIdText = string.Empty;
    public string CustomerIdText
    {
        get => _customerIdText;
        set => SetProperty(ref _customerIdText, value);
    }

    private string _serviceIdText = string.Empty;
    public string ServiceIdText
    {
        get => _serviceIdText;
        set => SetProperty(ref _serviceIdText, value);
    }

    private AvailabilitySlotModel? _selectedSlot;
    public AvailabilitySlotModel? SelectedSlot
    {
        get => _selectedSlot;
        set => SetProperty(ref _selectedSlot, value);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await _realtimeClient.StartAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task LoadAvailabilityAsync()
    {
        if (!Guid.TryParse(ServiceIdText, out var serviceId))
        {
            throw new InvalidOperationException("A valid Service ID is required.");
        }

        var start = DateTime.UtcNow.Date.AddHours(8);
        var end = start.AddHours(12);
        var results = await _apiClient.GetAvailabilityAsync(serviceId, start, end, CancellationToken.None);

        Slots.Clear();
        foreach (var slot in results)
        {
            Slots.Add(slot);
        }

        StatusMessage = $"Loaded {Slots.Count} available slots.";
    }

    [RelayCommand]
    private async Task CreateBookingAsync()
    {
        if (SelectedSlot is null)
        {
            throw new InvalidOperationException("Select an available slot.");
        }

        if (!Guid.TryParse(CustomerIdText, out var customerId))
        {
            throw new InvalidOperationException("A valid Customer ID is required.");
        }

        if (!Guid.TryParse(ServiceIdText, out var serviceId))
        {
            throw new InvalidOperationException("A valid Service ID is required.");
        }

        var bookingId = await _apiClient.CreateBookingAsync(new CreateBookingDraft
        {
            CustomerId = customerId,
            ServiceId = serviceId,
            StaffId = SelectedSlot.StaffId,
            ResourceId = SelectedSlot.ResourceId,
            StartUtc = SelectedSlot.StartUtc,
            EndUtc = SelectedSlot.EndUtc
        }, CancellationToken.None);

        StatusMessage = $"Created booking {bookingId}.";
    }
}
