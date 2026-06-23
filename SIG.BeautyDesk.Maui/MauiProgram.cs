using Microsoft.Extensions.Logging;
using SIG.BeautyDesk.Maui.Services;
using SIG.BeautyDesk.Maui.ViewModels;

namespace SIG.BeautyDesk.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddHttpClient<BeautyDeskApiClient>(client =>
		{
			client.BaseAddress = new Uri("http://localhost:5000");
		});
		builder.Services.AddSingleton<ReceptionRealtimeClient>();
		builder.Services.AddTransient<ReceptionViewModel>();
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
