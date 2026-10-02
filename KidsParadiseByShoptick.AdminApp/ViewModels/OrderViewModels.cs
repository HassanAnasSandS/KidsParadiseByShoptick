using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private const int PageSize = 30;

    private readonly AdminApiService _api;
    private readonly PagedListLoadCoordinator _load = new();
    private CancellationTokenSource? _searchDebounce;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool isLoadingMore;

    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusFilter = "All";
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<OrderModel> Items { get; } = [];
    public ObservableCollection<StatusFilterOption> StatusOptions { get; } = [];

    public OrdersViewModel(AdminApiService api) => _api = api;

    [RelayCommand]
    async Task OpenAlertsAsync() => await Shell.Current.GoToAsync("//notifications");

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (Items.Count > 0 || IsBusy || IsLoadingMore)
            return;

        await ReloadAsync();
    }

    bool CanLoad() => PagedListLoadCoordinator.CanReload(IsBusy, IsLoadingMore);

    [RelayCommand(CanExecute = nameof(CanLoad))]
    async Task LoadAsync()
    {
        IsRefreshing = true;
        try
        {
            await ReloadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    async Task ReloadAsync()
    {
        await _load.RunExclusiveAsync(async () =>
        {
            _load.BeginReload();
            HasMoreItems = false;
            ErrorMessage = null;
            StatusText = string.Empty;
            Items.Clear();
            LoadMoreCommand.NotifyCanExecuteChanged();
            await LoadStatusCountsAsync();
            await LoadNextPageCoreAsync(isRefresh: true);
        });
    }

    bool CanLoadMore() => PagedListLoadCoordinator.CanLoadMore(HasMoreItems, IsBusy, IsLoadingMore, Items.Count);

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    async Task LoadMoreAsync()
    {
        await _load.RunExclusiveAsync(async () =>
        {
            if (!PagedListLoadCoordinator.CanLoadMore(HasMoreItems, IsBusy, IsLoadingMore, Items.Count))
                return;

            await LoadNextPageCoreAsync(isRefresh: false);
        });
    }

    async Task LoadNextPageCoreAsync(bool isRefresh)
    {
        try
        {
            if (isRefresh) IsBusy = true;
            else IsLoadingMore = true;

            var result = await _api.GetOrdersPagedAsync(
                _load.CurrentPage,
                PageSize,
                StatusFilter,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                "newest");

            foreach (var order in result.Items)
                Items.Add(order);

            HasMoreItems = _load.CompletePage(Items.Count, result.TotalCount);
            StatusText = $"Showing {Items.Count} of {result.TotalCount} orders";
            LoadMoreCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = string.Empty;
            if (ex is UnauthorizedAccessException)
                await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_load.SuppressFilterReload)
            return;

        ScheduleFilterReload();
    }

    partial void OnStatusFilterChanged(string value)
    {
        UpdateStatusSelection();
        if (_load.SuppressFilterReload)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (StatusFilter != "All")
                StatusFilter = "All";
            return;
        }

        _ = ReloadAsync();
    }

    async Task LoadStatusCountsAsync()
    {
        try
        {
            var counts = await _api.GetOrderStatusCountsAsync();
            var current = StatusFilter;

            _load.SuppressFilters(true);
            StatusOptions.Clear();
            StatusOptions.Add(CreateStatusOption("All", counts.Total));
            StatusOptions.Add(CreateStatusOption("Pending", counts.Pending));
            StatusOptions.Add(CreateStatusOption("Confirmed", counts.Confirmed));
            StatusOptions.Add(CreateStatusOption("Shipped", counts.Shipped));
            StatusOptions.Add(CreateStatusOption("Delivered", counts.Delivered));
            StatusOptions.Add(CreateStatusOption("Cancelled", counts.Cancelled));
            StatusFilter = current;
            UpdateStatusSelection();
            _load.SuppressFilters(false);
        }
        catch
        {
            _load.SuppressFilters(false);
        }
    }

    static StatusFilterOption CreateStatusOption(string value, int count) =>
        new() { Value = value, Label = $"{value} ({count})" };

    void UpdateStatusSelection()
    {
        foreach (var option in StatusOptions)
            option.IsSelected = string.Equals(option.Value, StatusFilter, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    Task SelectStatusAsync(StatusFilterOption option)
    {
        if (option is null || string.Equals(StatusFilter, option.Value, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        StatusFilter = option.Value;
        return Task.CompletedTask;
    }

    void ScheduleFilterReload()
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, token);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (!token.IsCancellationRequested)
                        await ReloadAsync();
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [RelayCommand]
    async Task CreateAsync() => await Shell.Current.GoToAsync("create-order");

    [RelayCommand]
    async Task OpenAsync(OrderModel order) => await Shell.Current.GoToAsync($"order-detail?id={order.Id}");

    [RelayCommand]
    async Task OpenWhatsAppAsync(OrderModel order)
    {
        if (order is null || string.IsNullOrWhiteSpace(order.Whatsapp))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "Customer WhatsApp number is missing.", "OK");
            return;
        }

        try
        {
            var fullOrder = order.Items.Count > 0 ? order : await _api.GetOrderAsync(order.Id);
            await OrderWhatsAppHelper.OpenCustomerChatAsync(fullOrder);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task DeleteAsync(OrderModel? order)
    {
        if (order is null) return;
        if (!order.IsPending)
        {
            await Shell.Current.DisplayAlert("Not allowed", "Only pending orders can be deleted.", "OK");
            return;
        }

        var ok = await Shell.Current.DisplayAlert(
            "Delete order?",
            $"Delete {order.OrderNumber}? Toys in this order will become available again on the website.",
            "Delete",
            "Cancel");
        if (!ok) return;

        try
        {
            await _api.DeleteOrderAsync(order.Id);
            await ReloadAsync();
            await LoadStatusCountsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

public partial class OrderDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int _id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PayableAfterDiscount))]
    [NotifyPropertyChangedFor(nameof(ShowPaymentDetails))]
    [NotifyPropertyChangedFor(nameof(CanOpenWhatsApp))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyCanExecuteChangedFor(nameof(OpenWhatsAppCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private OrderModel? order;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string selectedStatus = "Pending";
    [ObservableProperty] private string trackingNumber = string.Empty;
    [ObservableProperty] private string advanceText = string.Empty;
    [ObservableProperty] private string discountText = string.Empty;

    public ObservableCollection<string> StatusOptions { get; } =
        ["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"];

    public decimal PayableAfterDiscount =>
        Order is null ? 0 : Order.Total - (Order.DiscountAmount ?? 0);

    public bool ShowPaymentDetails =>
        Order?.Status is "Confirmed" or "Shipped" or "Delivered";

    public bool CanOpenWhatsApp =>
        !string.IsNullOrWhiteSpace(Order?.Whatsapp);

    public bool CanDelete => Order?.IsPending == true;

    public OrderDetailViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj))
            int.TryParse(idObj?.ToString(), out _id);
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        try
        {
            IsBusy = true;
            Order = await _api.GetOrderAsync(_id);
            if (Order is null) return;
            SelectedStatus = Order.Status;
            TrackingNumber = Order.TrackingNumber ?? string.Empty;
            AdvanceText = Order.AdvanceAmount?.ToString("0") ?? string.Empty;
            DiscountText = Order.DiscountAmount?.ToString("0") ?? string.Empty;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenWhatsApp))]
    async Task OpenWhatsAppAsync()
    {
        if (Order is null) return;
        try
        {
            await OrderWhatsAppHelper.OpenCustomerChatAsync(Order);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task CopyNameAsync() => await CopyToClipboardAsync(Order?.CustomerName, "Name");

    [RelayCommand]
    async Task CopyWhatsappAsync() => await CopyToClipboardAsync(Order?.Whatsapp, "WhatsApp");

    [RelayCommand]
    async Task CopyCityAsync() => await CopyToClipboardAsync(Order?.City, "City");

    [RelayCommand]
    async Task CopyAddressAsync() => await CopyToClipboardAsync(Order?.Address, "Address");

    static async Task CopyToClipboardAsync(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            await Shell.Current.DisplayAlert("Copy", $"{label} is empty.", "OK");
            return;
        }

        await Clipboard.Default.SetTextAsync(value.Trim());
        await Toast.Make($"{label} copied").Show();
    }

    [RelayCommand]
    async Task SaveStatusAsync()
    {
        if (Order is null) return;
        try
        {
            IsBusy = true;
            decimal? advance = string.IsNullOrWhiteSpace(AdvanceText) ? null : decimal.Parse(AdvanceText);
            decimal? discount = string.IsNullOrWhiteSpace(DiscountText) ? null : decimal.Parse(DiscountText);
            await _api.UpdateOrderStatusAsync(_id, new
            {
                status = SelectedStatus,
                trackingNumber = string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber.Trim(),
                advanceAmount = advance,
                discountAmount = discount,
            });
            await AppearingAsync();
            await Shell.Current.DisplayAlert("Saved", "Order status updated.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task EditAsync()
    {
        if (Order?.Status == "Cancelled")
        {
            await Shell.Current.DisplayAlert("Not allowed", "Cancelled orders cannot be edited.", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"order-edit?id={_id}");
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    async Task DeleteAsync()
    {
        if (Order is null || !CanDelete) return;

        var ok = await Shell.Current.DisplayAlert(
            "Delete order?",
            $"Delete {Order.OrderNumber}? Toys in this order will become available again on the website.",
            "Delete",
            "Cancel");
        if (!ok) return;

        try
        {
            IsBusy = true;
            await _api.DeleteOrderAsync(_id);
            await Shell.Current.DisplayAlert("Deleted", "Order deleted. Toys are available again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class CreateOrderViewModel : ObservableObject
{
    private readonly AdminApiService _api;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private CancellationTokenSource? _searchDebounce;

    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenWhatsApp))]
    [NotifyCanExecuteChangedFor(nameof(OpenWhatsAppCommand))]
    private string whatsapp = string.Empty;
    [ObservableProperty] private string city = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string toySearch = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private AffiliatePartnerModel? selectedAffiliate;

    public bool CanOpenWhatsApp => !string.IsNullOrWhiteSpace(Whatsapp);

    public ObservableCollection<ToyListModel> AvailableToys { get; } = [];
    public ObservableCollection<ToyListModel> SelectedToys { get; } = [];
    public ObservableCollection<AffiliatePartnerModel> Affiliates { get; } = [];

    public CreateOrderViewModel(AdminApiService api) => _api = api;

    [RelayCommand(CanExecute = nameof(CanOpenWhatsApp))]
    async Task OpenWhatsAppAsync()
    {
        if (string.IsNullOrWhiteSpace(Whatsapp))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "Enter customer WhatsApp number first.", "OK");
            return;
        }

        try
        {
            var items = SelectedToys.Select(t => new OrderItemModel
            {
                ToyId = t.Id,
                ToyName = t.Name,
                Price = t.EffectivePrice,
                ImageUrl = string.IsNullOrWhiteSpace(t.PrimaryImage) ? null : t.PrimaryImage,
            }).ToList();
            var subTotal = items.Sum(i => i.Price);
            var draft = new OrderModel
            {
                OrderNumber = "(new order)",
                Status = "Pending",
                CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? "Customer" : CustomerName.Trim(),
                Whatsapp = Whatsapp.Trim(),
                City = City.Trim(),
                Address = Address.Trim(),
                SubTotal = subTotal,
                DeliveryCharge = 0,
                Total = subTotal,
                Items = items,
            };
            await OrderWhatsAppHelper.OpenCustomerChatAsync(draft);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        await RefreshAvailableAsync();
        try
        {
            var partners = await _api.GetAffiliatesAsync(isActive: true);
            Affiliates.Clear();
            Affiliates.Add(new AffiliatePartnerModel { Id = 0, Name = "(None)", Code = string.Empty });
            foreach (var p in partners)
                Affiliates.Add(p);
            SelectedAffiliate ??= Affiliates.FirstOrDefault();
        }
        catch
        {
            // Optional — order can still be created without affiliate list.
        }
    }

    async Task RefreshAvailableAsync()
    {
        await _refreshGate.WaitAsync();
        try
        {
            var selectedIds = SelectedToys.Select(t => t.Id).ToHashSet();
            var result = await _api.GetToysPagedAsync(
                1, 50, search: string.IsNullOrWhiteSpace(ToySearch) ? null : ToySearch, isSold: false);
            AvailableToys.Clear();
            foreach (var t in result.Items.Where(x => !selectedIds.Contains(x.Id)))
                AvailableToys.Add(t);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    partial void OnToySearchChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, token);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (!token.IsCancellationRequested)
                        await RefreshAvailableAsync();
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [RelayCommand]
    void AddToy(ToyListModel toy)
    {
        SelectedToys.Add(toy);
        _ = RefreshAvailableAsync();
    }

    [RelayCommand]
    void RemoveToy(ToyListModel toy)
    {
        SelectedToys.Remove(toy);
        _ = RefreshAvailableAsync();
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (SelectedToys.Count == 0
            || string.IsNullOrWhiteSpace(CustomerName)
            || string.IsNullOrWhiteSpace(Whatsapp)
            || string.IsNullOrWhiteSpace(City)
            || string.IsNullOrWhiteSpace(Address))
        {
            await Shell.Current.DisplayAlert("Validation", "Fill all customer fields and select at least one toy.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _api.CreateOrderAsync(new
            {
                name = CustomerName.Trim(),
                whatsapp = Whatsapp.Trim(),
                city = City.Trim(),
                address = Address.Trim(),
                toyIds = SelectedToys.Select(t => t.Id).ToList(),
                affiliateCode = SelectedAffiliate is { Id: > 0 } ? SelectedAffiliate.Code : null,
            });
            await Shell.Current.DisplayAlert("Order Created", $"Order {result.OrderNumber} — Rs. {result.Total:N0}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class OrderEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private int _id;
    private CancellationTokenSource? _searchDebounce;
    private bool _delivered;
    private OrderModel? _loadedOrder;

    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenWhatsApp))]
    [NotifyCanExecuteChangedFor(nameof(OpenWhatsAppCommand))]
    private string whatsapp = string.Empty;
    [ObservableProperty] private string city = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string deliveryChargeText = "0";
    [ObservableProperty] private string advanceText = string.Empty;
    [ObservableProperty] private string discountText = string.Empty;
    [ObservableProperty] private string trackingNumber = string.Empty;
    [ObservableProperty] private string toySearch = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool canEditToys = true;

    public bool CanOpenWhatsApp => !string.IsNullOrWhiteSpace(Whatsapp);

    public ObservableCollection<ToyListModel> SelectedToys { get; } = [];
    public ObservableCollection<ToyListModel> AvailableToys { get; } = [];

    public OrderEditViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj))
            int.TryParse(idObj?.ToString(), out _id);
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        var order = await _api.GetOrderAsync(_id);
        _loadedOrder = order;
        CustomerName = order.CustomerName;
        Whatsapp = order.Whatsapp;
        City = order.City;
        Address = order.Address;
        DeliveryChargeText = order.DeliveryCharge.ToString("0");
        AdvanceText = order.AdvanceAmount?.ToString("0") ?? string.Empty;
        DiscountText = order.DiscountAmount?.ToString("0") ?? string.Empty;
        TrackingNumber = order.TrackingNumber ?? string.Empty;
        _delivered = order.Status == "Delivered";
        CanEditToys = order.Status is not "Cancelled" and not "Delivered";

        SelectedToys.Clear();
        foreach (var item in order.Items)
        {
            SelectedToys.Add(new ToyListModel
            {
                Id = item.ToyId,
                Name = item.ToyName,
                Price = item.Price,
                ImageUrls = item.ImageUrl is null ? [] : [item.ImageUrl],
            });
        }
        await RefreshAvailableAsync();
    }

    [RelayCommand(CanExecute = nameof(CanOpenWhatsApp))]
    async Task OpenWhatsAppAsync()
    {
        if (string.IsNullOrWhiteSpace(Whatsapp))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "Customer WhatsApp number is missing.", "OK");
            return;
        }

        try
        {
            var order = _loadedOrder ?? await _api.GetOrderAsync(_id);
            order.CustomerName = CustomerName.Trim();
            order.Whatsapp = Whatsapp.Trim();
            order.City = City.Trim();
            order.Address = Address.Trim();
            order.TrackingNumber = string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber.Trim();
            if (decimal.TryParse(DeliveryChargeText, out var delivery))
                order.DeliveryCharge = delivery;
            if (decimal.TryParse(AdvanceText, out var advance))
                order.AdvanceAmount = advance;
            if (decimal.TryParse(DiscountText, out var discount))
                order.DiscountAmount = discount;

            await OrderWhatsAppHelper.OpenCustomerChatAsync(order);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    async Task RefreshAvailableAsync()
    {
        if (!CanEditToys) return;
        await _refreshGate.WaitAsync();
        try
        {
            if (!CanEditToys) return;
            var selectedIds = SelectedToys.Select(t => t.Id).ToHashSet();
            var result = await _api.GetToysPagedAsync(
                1, 50, search: string.IsNullOrWhiteSpace(ToySearch) ? null : ToySearch, isSold: false);
            AvailableToys.Clear();
            foreach (var t in result.Items.Where(x => !selectedIds.Contains(x.Id)))
                AvailableToys.Add(t);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    partial void OnToySearchChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, token);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (!token.IsCancellationRequested)
                        await RefreshAvailableAsync();
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [RelayCommand]
    void AddToy(ToyListModel toy)
    {
        if (!CanEditToys) return;
        SelectedToys.Add(toy);
        _ = RefreshAvailableAsync();
    }

    [RelayCommand]
    void RemoveToy(ToyListModel toy)
    {
        if (!CanEditToys) return;
        SelectedToys.Remove(toy);
        _ = RefreshAvailableAsync();
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            decimal? advance = string.IsNullOrWhiteSpace(AdvanceText) ? null : decimal.Parse(AdvanceText);
            decimal? discount = string.IsNullOrWhiteSpace(DiscountText) ? null : decimal.Parse(DiscountText);
            if (!decimal.TryParse(DeliveryChargeText, out var deliveryCharge))
            {
                await Shell.Current.DisplayAlert("Validation", "Invalid delivery charge.", "OK");
                return;
            }
            await _api.UpdateOrderAsync(_id, new
            {
                customerName = CustomerName.Trim(),
                whatsapp = Whatsapp.Trim(),
                city = City.Trim(),
                address = Address.Trim(),
                deliveryCharge,
                advanceAmount = advance,
                discountAmount = discount,
                trackingNumber = string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber.Trim(),
                toyIds = SelectedToys.Select(t => t.Id).ToList(),
            });
            await Shell.Current.DisplayAlert("Saved", "Order updated.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
