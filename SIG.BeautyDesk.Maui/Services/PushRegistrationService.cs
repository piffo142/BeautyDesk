namespace SIG.BeautyDesk.Maui.Services;

public sealed class PushRegistrationService(BeautyDeskApiClient apiClient)
{
    public async Task RegisterAsync(Guid staffId, CancellationToken cancellationToken)
    {
        var currentPlatform = DeviceInfo.Platform;
        var platform = 4;
        if (currentPlatform == DevicePlatform.Android)
        {
            platform = 1;
        }
        else if (currentPlatform == DevicePlatform.iOS)
        {
            platform = 2;
        }
        else if (currentPlatform == DevicePlatform.MacCatalyst)
        {
            platform = 3;
        }

        var pushToken = $"{platform}-{DeviceInfo.Name}-{DeviceInfo.Model}";
        await apiClient.RegisterPushDeviceAsync(staffId, platform, pushToken, cancellationToken);
    }
}
