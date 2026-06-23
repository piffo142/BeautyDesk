using Microsoft.Extensions.DependencyInjection;

namespace SIG.BeautyDesk.Maui;

public partial class App : Application
{
	private readonly MainPage _receptionPage;
	private readonly StaffAgendaPage _staffAgendaPage;

	public App(MainPage receptionPage, StaffAgendaPage staffAgendaPage)
	{
		InitializeComponent();
		_receptionPage = receptionPage;
		_staffAgendaPage = staffAgendaPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var idiom = DeviceInfo.Idiom;
		var isMobile = idiom == DeviceIdiom.Phone || idiom == DeviceIdiom.Tablet;
		return new Window(isMobile ? _staffAgendaPage : _receptionPage);
	}
}