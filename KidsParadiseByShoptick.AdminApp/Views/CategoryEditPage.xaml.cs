using KidsParadiseByShoptick.AdminApp.ViewModels;

namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class CategoryEditPage : ContentPage
{
    public CategoryEditPage(CategoryEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CategoryEditViewModel vm && vm.AppearingCommand.CanExecute(null))
            _ = vm.AppearingCommand.ExecuteAsync(null);
    }
}
