using KidsParadiseByShoptick.AdminApp.Services;
using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp;

public partial class AppShell : Shell
{
    private readonly ShellViewModel _shellVm;

    public AppShell(ShellViewModel shellVm)
    {
        InitializeComponent();
        _shellVm = shellVm;
        BindingContext = shellVm;

        Routing.RegisterRoute("category-edit", typeof(Views.CategoryEditPage));
        Routing.RegisterRoute("toy-edit", typeof(Views.ToyEditPage));
        Routing.RegisterRoute("order-detail", typeof(Views.OrderDetailPage));
        Routing.RegisterRoute("order-edit", typeof(Views.OrderEditPage));
        Routing.RegisterRoute("create-order", typeof(Views.CreateOrderPage));
        Routing.RegisterRoute("review-edit", typeof(Views.ReviewEditPage));
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (_shellVm.LogoutCommand.CanExecute(null))
            await _shellVm.LogoutCommand.ExecuteAsync(null);
    }
}
