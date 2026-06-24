using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class OrdersPage : ContentPage
{
    public OrdersPage(OrdersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is OrdersViewModel vm && vm.AppearingCommand.CanExecute(null))
            _ = vm.AppearingCommand.ExecuteAsync(null);
    }
}
