using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly AdminApiService _api;
    private readonly AuthSession _session;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string lastUpdatedText = string.Empty;
    [ObservableProperty] private string filterSummary = "All time";

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private bool isFilterActive;

    [ObservableProperty] private int totalToys;
    [ObservableProperty] private int totalAvailableToys;
    [ObservableProperty] private int totalSoldToys;
    [ObservableProperty] private int totalToysOnSale;
    [ObservableProperty] private int totalToysOnRegular;
    [ObservableProperty] private string totalToysAmountText = "Rs. 0";
    [ObservableProperty] private string availableToysAmountText = "Rs. 0";
    [ObservableProperty] private string soldToysAmountText = "Rs. 0";
    [ObservableProperty] private string regularToysAmountText = "Rs. 0";
    [ObservableProperty] private string onSaleToysAmountText = "Rs. 0";

    [ObservableProperty] private int totalCustomers;
    [ObservableProperty] private int totalDeliveredOrders;
    [ObservableProperty] private string allDeliveredOrdersTotalAmountText = "Rs. 0";

    public DashboardViewModel(AdminApiService api, AuthSession session)
    {
        _api = api;
        _session = session;
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (!_session.IsLoggedIn || IsBusy)
            return;

        await LoadAsync(refreshing: false);
    }

    [RelayCommand]
    async Task RefreshAsync()
    {
        if (!_session.IsLoggedIn || IsBusy)
            return;

        await LoadAsync(refreshing: true);
    }

    [RelayCommand]
    async Task ApplyFilterAsync()
    {
        if (FromDate.Date > ToDate.Date)
        {
            ErrorMessage = "From date cannot be after To date.";
            return;
        }

        IsFilterActive = true;
        await LoadAsync(refreshing: false);
    }

    [RelayCommand]
    async Task ClearFilterAsync()
    {
        IsFilterActive = false;
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        await LoadAsync(refreshing: false);
    }

    [RelayCommand]
    async Task OpenToysAsync() => await Shell.Current.GoToAsync("//toys");

    [RelayCommand]
    async Task OpenOrdersAsync() => await Shell.Current.GoToAsync("//orders");

    private async Task LoadAsync(bool refreshing)
    {
        ErrorMessage = null;

        try
        {
            if (refreshing)
                IsRefreshing = true;
            else
                IsBusy = true;

            DateTime? from = IsFilterActive ? FromDate.Date : null;
            DateTime? to = IsFilterActive ? ToDate.Date : null;

            var stats = await _api.GetDashboardAsync(from, to);

            TotalToys = stats.TotalToys;
            TotalAvailableToys = stats.TotalAvailableToys;
            TotalSoldToys = stats.TotalSoldToys;
            TotalToysOnSale = stats.TotalToysOnSale;
            TotalToysOnRegular = stats.TotalToysOnRegular;
            TotalToysAmountText = FormatHelpers.Price(stats.AllToysTotalAmount);
            AvailableToysAmountText = FormatHelpers.Price(stats.AvailableToysTotalAmount);
            SoldToysAmountText = FormatHelpers.Price(stats.AllSoldToysTotalAmount);
            RegularToysAmountText = FormatHelpers.Price(stats.RegularToysTotalAmount);
            OnSaleToysAmountText = FormatHelpers.Price(stats.OnSaleToysTotalAmount);
            TotalCustomers = stats.TotalCustomers;
            TotalDeliveredOrders = stats.TotalDeliveredOrders;
            AllDeliveredOrdersTotalAmountText = FormatHelpers.Price(stats.AllDeliveredOrdersTotalAmount);

            FilterSummary = IsFilterActive
                ? $"{FromDate:dd MMM yyyy} – {ToDate:dd MMM yyyy}"
                : "All time";

            LastUpdatedText = $"Updated {DateTime.Now:hh:mm tt}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            if (ex is UnauthorizedAccessException)
                await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
