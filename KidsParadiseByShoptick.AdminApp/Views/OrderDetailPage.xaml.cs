using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class OrderDetailPage : ContentPage
{
    public OrderDetailViewModel ViewModel { get; }

    public OrderDetailPage(OrderDetailViewModel vm)
    {
        ViewModel = vm;
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.AppearingCommand.CanExecute(null))
            _ = ViewModel.AppearingCommand.ExecuteAsync(null);
    }
}
