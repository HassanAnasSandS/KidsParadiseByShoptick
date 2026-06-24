using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class ToysViewModel : ObservableObject
{
    private const int PageSize = 30;

    private readonly AdminApiService _api;
    private readonly Dictionary<string, int> _categoryIds = new(StringComparer.OrdinalIgnoreCase);
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
    [ObservableProperty] private string categoryFilter = "All";
    [ObservableProperty] private string statusFilter = "All";
    [ObservableProperty] private string saleFilter = "All";
    [ObservableProperty] private string sortFilter = "Name (A-Z)";
    [ObservableProperty] private bool showFilters;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<ToyListModel> Items { get; } = [];
    public ObservableCollection<string> CategoryOptions { get; } = ["All"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Available", "Sold"];
    public ObservableCollection<string> SaleOptions { get; } = ["All", "On Sale", "Regular"];
    public ObservableCollection<string> SortOptions { get; } =
        ["Name (A-Z)", "Price: Low to High", "Price: High to Low"];

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText)
        || (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "All")
        || StatusFilter != "All"
        || SaleFilter != "All"
        || SortFilter != "Name (A-Z)";

    public ToysViewModel(AdminApiService api) => _api = api;

    [RelayCommand]
    async Task AppearingAsync()
    {
        await LoadCategoriesAsync();
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

    async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _api.GetCategoriesAsync();
            _categoryIds.Clear();
            CategoryOptions.Clear();
            CategoryOptions.Add("All");
            foreach (var c in categories.OrderBy(x => x.Name))
            {
                if (string.IsNullOrWhiteSpace(c.Name)) continue;
                _categoryIds[c.Name] = c.Id;
                CategoryOptions.Add(c.Name);
            }

            if (string.IsNullOrWhiteSpace(CategoryFilter)
                || (CategoryFilter != "All" && !_categoryIds.ContainsKey(CategoryFilter)))
            {
                if (CategoryFilter != "All")
                    CategoryFilter = "All";
            }
        }
        catch
        {
            // Category filter is optional; list can still load.
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
            if (isRefresh)
                IsBusy = true;
            else
                IsLoadingMore = true;

            ErrorMessage = null;
            var categoryId = ResolveCategoryFilterId();

            var result = await _api.GetToysPagedAsync(
                _load.CurrentPage,
                PageSize,
                categoryId,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                ResolveIsSold(),
                ResolveOnSale(),
                ResolveSort());

            foreach (var toy in result.Items)
                Items.Add(toy);

            HasMoreItems = _load.CompletePage(Items.Count, result.TotalCount);
            StatusText = $"Showing {Items.Count} of {result.TotalCount} toys";
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
        NotifyHasActiveFiltersChanged();
        if (_load.SuppressFilterReload)
            return;

        ScheduleFilterReload();
    }

    partial void OnCategoryFilterChanged(string value)
    {
        if (_load.SuppressFilterReload)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (CategoryFilter != "All")
                CategoryFilter = "All";
            return;
        }

        NotifyHasActiveFiltersChanged();
        _ = ReloadAsync();
    }

    partial void OnStatusFilterChanged(string value)
    {
        if (_load.SuppressFilterReload)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (StatusFilter != "All")
                StatusFilter = "All";
            return;
        }

        NotifyHasActiveFiltersChanged();
        _ = ReloadAsync();
    }

    partial void OnSaleFilterChanged(string value)
    {
        if (_load.SuppressFilterReload)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (SaleFilter != "All")
                SaleFilter = "All";
            return;
        }

        NotifyHasActiveFiltersChanged();
        _ = ReloadAsync();
    }

    partial void OnSortFilterChanged(string value)
    {
        if (_load.SuppressFilterReload)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (SortFilter != "Name (A-Z)")
                SortFilter = "Name (A-Z)";
            return;
        }

        NotifyHasActiveFiltersChanged();
        _ = ReloadAsync();
    }

    void NotifyHasActiveFiltersChanged() => OnPropertyChanged(nameof(HasActiveFilters));

    int? ResolveCategoryFilterId()
    {
        if (string.IsNullOrWhiteSpace(CategoryFilter) || CategoryFilter == "All")
            return null;
        return _categoryIds.TryGetValue(CategoryFilter, out var id) ? id : null;
    }

    bool? ResolveIsSold() => StatusFilter switch
    {
        "Available" => false,
        "Sold" => true,
        _ => null,
    };

    bool? ResolveOnSale() => SaleFilter switch
    {
        "On Sale" => true,
        "Regular" => false,
        _ => null,
    };

    string ResolveSort() => SortFilter switch
    {
        "Price: Low to High" => "price-low",
        "Price: High to Low" => "price-high",
        _ => "name",
    };

    [RelayCommand]
    void ToggleFilters() => ShowFilters = !ShowFilters;

    [RelayCommand]
    async Task ClearFiltersAsync()
    {
        _searchDebounce?.Cancel();
        _load.SuppressFilters();
        SearchText = string.Empty;
        CategoryFilter = "All";
        StatusFilter = "All";
        SaleFilter = "All";
        SortFilter = "Name (A-Z)";
        NotifyHasActiveFiltersChanged();
        await ReloadAsync();
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
            catch (TaskCanceledException)
            {
                // Ignore debounce cancel.
            }
        }, token);
    }

    [RelayCommand]
    async Task AddAsync() => await Shell.Current.GoToAsync("toy-edit");

    [RelayCommand]
    async Task EditAsync(ToyListModel item) => await Shell.Current.GoToAsync($"toy-edit?id={item.Id}");

    [RelayCommand]
    async Task DeleteAsync(ToyListModel item)
    {
        if (!await Shell.Current.DisplayAlert("Delete", $"Delete toy \"{item.Name}\"?", "Delete", "Cancel"))
            return;
        try
        {
            await _api.DeleteToyAsync(item.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

public partial class ToyEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int? _id;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string priceText = "0";
    [ObservableProperty] private string salePriceText = string.Empty;
    [ObservableProperty] private CategoryModel? selectedCategory;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string title = "New Toy";

    public ObservableCollection<CategoryModel> Categories { get; } = [];
    public ObservableCollection<ToyImageItem> Images { get; } = [];

    public ToyEditViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
            _id = id;
        Title = _id.HasValue ? "Edit Toy" : "New Toy";
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        var categories = await _api.GetCategoriesAsync();
        Categories.Clear();
        foreach (var c in categories.OrderBy(x => x.Name))
            Categories.Add(c);

        if (!_id.HasValue)
        {
            SelectedCategory = Categories.FirstOrDefault();
            return;
        }

        var toy = await _api.GetToyAsync(_id.Value);
        Name = toy.Name;
        PriceText = toy.Price.ToString("0");
        SalePriceText = toy.SalePrice?.ToString("0") ?? string.Empty;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == toy.CategoryId) ?? Categories.FirstOrDefault();
        Images.Clear();
        for (var i = 0; i < toy.ImageUrls.Count; i++)
        {
            Images.Add(new ToyImageItem
            {
                Path = i < toy.ImagePaths.Count ? toy.ImagePaths[i] : string.Empty,
                Url = toy.ImageUrls[i],
            });
        }
    }

    [RelayCommand]
    async Task PickImagesAsync()
    {
        var files = await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Toy images" });
        if (files is null) return;
        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            var result = await _api.UploadAsync(ms, file.FileName, "toys");
            Images.Add(new ToyImageItem { Path = result.Path, Url = result.Url });
        }
    }

    [RelayCommand]
    void RemoveImage(ToyImageItem item) => Images.Remove(item);

    [RelayCommand]
    async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || SelectedCategory is null)
        {
            await Shell.Current.DisplayAlert("Validation", "Name and category are required.", "OK");
            return;
        }

        if (!decimal.TryParse(PriceText, out var price))
        {
            await Shell.Current.DisplayAlert("Validation", "Invalid price.", "OK");
            return;
        }

        decimal? salePrice = null;
        if (!string.IsNullOrWhiteSpace(SalePriceText))
        {
            if (!decimal.TryParse(SalePriceText, out var sp))
            {
                await Shell.Current.DisplayAlert("Validation", "Invalid sale price.", "OK");
                return;
            }
            salePrice = sp;
        }

        var payload = new
        {
            categoryId = SelectedCategory.Id,
            name = Name.Trim(),
            price,
            salePrice,
            imagePaths = Images.Select(i => i.Path).ToList(),
        };

        try
        {
            IsBusy = true;
            if (_id.HasValue)
                await _api.UpdateToyAsync(_id.Value, payload);
            else
                await _api.CreateToyAsync(payload);
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

public class ToyImageItem
{
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
