using SIG.BeautyDesk.Maui.ViewModels;

namespace SIG.BeautyDesk.Maui;

public partial class MainPage : ContentPage
{
    public MainPage(ReceptionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
