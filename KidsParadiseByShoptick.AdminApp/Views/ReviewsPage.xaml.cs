using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class ReviewsPage : ContentPage
{
    public ReviewsPage(ReviewsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ReviewsViewModel vm && vm.AppearingCommand.CanExecute(null))
            _ = vm.AppearingCommand.ExecuteAsync(null);
    }
}
