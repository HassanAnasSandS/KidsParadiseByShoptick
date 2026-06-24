using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NotificationsViewModel vm && vm.AppearingCommand.CanExecute(null))
            _ = vm.AppearingCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is NotificationsViewModel vm && vm.DisappearingCommand.CanExecute(null))
            vm.DisappearingCommand.Execute(null);
    }
}
