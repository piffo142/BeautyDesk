using SIG.BeautyDesk.Maui.ViewModels;

namespace SIG.BeautyDesk.Maui;

public partial class StaffAgendaPage : ContentPage
{
    public StaffAgendaPage(StaffAgendaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
