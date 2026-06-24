using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class CreateOrderPage : ContentPage
{
    public CreateOrderPage(CreateOrderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CreateOrderViewModel vm && vm.AppearingCommand.CanExecute(null))
            _ = vm.AppearingCommand.ExecuteAsync(null);
    }
}
