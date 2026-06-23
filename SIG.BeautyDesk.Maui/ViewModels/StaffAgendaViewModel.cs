using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIG.BeautyDesk.Maui.Models;
using SIG.BeautyDesk.Maui.Services;

namespace SIG.BeautyDesk.Maui.ViewModels;

public partial class StaffAgendaViewModel : ObservableObject
{
    private readonly BeautyDeskApiClient _apiClient;
    private readonly PushRegistrationService _pushRegistrationService;

    public StaffAgendaViewModel(BeautyDeskApiClient apiClient, PushRegistrationService pushRegistrationService)
    {
        _apiClient = apiClient;
        _pushRegistrationService = pushRegistrationService;
    }

    public ObservableCollection<StaffAgendaBookingModel> Bookings { get; } = [];

    private string _staffIdText = string.Empty;
    public string StaffIdText
    {
        get => _staffIdText;
        set => SetProperty(ref _staffIdText, value);
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private StaffAgendaBookingModel? _selectedBooking;
    public StaffAgendaBookingModel? SelectedBooking
    {
        get => _selectedBooking;
        set => SetProperty(ref _selectedBooking, value);
    }

    [RelayCommand]
    private async Task LoadAgendaAsync()
    {
        if (!Guid.TryParse(StaffIdText, out var staffId))
        {
            throw new InvalidOperationException("A valid staff id is required.");
        }

        var agenda = await _apiClient.GetStaffAgendaAsync(staffId, DateTime.UtcNow.Date, CancellationToken.None);
        Bookings.Clear();
        foreach (var item in agenda)
        {
            Bookings.Add(item);
        }

        StatusMessage = $"Loaded {Bookings.Count} bookings.";
    }

    [RelayCommand]
    private async Task RegisterPushAsync()
    {
        if (!Guid.TryParse(StaffIdText, out var staffId))
        {
            throw new InvalidOperationException("A valid staff id is required.");
        }

        await _pushRegistrationService.RegisterAsync(staffId, CancellationToken.None);
        StatusMessage = "Push device registration submitted.";
    }

    [RelayCommand]
    private Task MarkArrivedAsync() => UpdateStatusAsync("Arrived");

    [RelayCommand]
    private Task MarkCompletedAsync() => UpdateStatusAsync("Completed");

    [RelayCommand]
    private Task MarkNoShowAsync() => UpdateStatusAsync("NoShow");

    private async Task UpdateStatusAsync(string status)
    {
        if (SelectedBooking is null)
        {
            throw new InvalidOperationException("Select a booking first.");
        }

        await _apiClient.UpdateBookingStatusAsync(SelectedBooking.BookingId, status, CancellationToken.None);
        SelectedBooking.Status = status;
        OnPropertyChanged(nameof(Bookings));
        StatusMessage = $"Booking {SelectedBooking.BookingId} updated to {status}.";
    }
}
