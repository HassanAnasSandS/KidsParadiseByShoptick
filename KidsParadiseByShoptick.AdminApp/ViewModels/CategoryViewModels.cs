using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AdminApiService _api;
    private readonly OrderNotificationService _notifications;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool rememberMe = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public LoginViewModel(AdminApiService api, AuthSession session, OrderNotificationService notifications)
    {
        _api = api;
        _notifications = notifications;
        Username = session.Username ?? string.Empty;
        RememberMe = session.RememberMe;
    }

    [RelayCommand]
    async Task LoginAsync()
    {
        if (IsBusy) return;
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter username and password.";
            return;
        }

        try
        {
            IsBusy = true;
            await OrderNotificationService.RequestPermissionAsync();
            await _api.LoginAsync(Username.Trim(), Password, RememberMe);
            _notifications.Start();
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class CategoriesViewModel : ObservableObject
{
    private const int PageSize = 30;

    private readonly AdminApiService _api;
    private readonly AuthSession _session;
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
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<CategoryModel> Items { get; } = [];

    public CategoriesViewModel(AdminApiService api, AuthSession session)
    {
        _api = api;
        _session = session;
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (!_session.IsLoggedIn || Items.Count > 0 || IsBusy || IsLoadingMore)
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

            var result = await _api.GetCategoriesPagedAsync(
                _load.CurrentPage,
                PageSize,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

            foreach (var item in result.Items.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                Items.Add(item);

            HasMoreItems = _load.CompletePage(Items.Count, result.TotalCount);
            StatusText = $"Showing {Items.Count} of {result.TotalCount} categories";
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
    async Task AddAsync() => await Shell.Current.GoToAsync("category-edit");

    [RelayCommand]
    async Task EditAsync(CategoryModel item) =>
        await Shell.Current.GoToAsync($"category-edit?id={item.Id}");

    [RelayCommand]
    async Task DeleteAsync(CategoryModel item)
    {
        if (!await Shell.Current.DisplayAlert("Delete", $"Delete category \"{item.Name}\"?", "Delete", "Cancel"))
            return;
        try
        {
            await _api.DeleteCategoryAsync(item.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

public partial class CategoryEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int? _id;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? imagePath;
    [ObservableProperty] private string? imageUrl;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string title = "New Category";

    public CategoryEditViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
            _id = id;
        Title = _id.HasValue ? "Edit Category" : "New Category";
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (!_id.HasValue) return;
        var cats = await _api.GetCategoriesAsync();
        var cat = cats.FirstOrDefault(c => c.Id == _id.Value);
        if (cat is null) return;
        Name = cat.Name;
        ImagePath = cat.ImagePath;
        ImageUrl = cat.ImageUrl;
    }

    [RelayCommand]
    async Task PickImageAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Category image" });
        if (file is null) return;
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        var result = await _api.UploadAsync(ms, file.FileName, "categories");
        ImagePath = result.Path;
        ImageUrl = result.Url;
    }

    [RelayCommand]
    void RemoveImage()
    {
        ImagePath = string.Empty;
        ImageUrl = null;
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Validation", "Name is required.", "OK");
            return;
        }
        try
        {
            IsBusy = true;
            var path = string.IsNullOrWhiteSpace(ImagePath) ? null : ImagePath;
            if (_id.HasValue)
                await _api.UpdateCategoryAsync(_id.Value, Name.Trim(), path);
            else
                await _api.CreateCategoryAsync(Name.Trim(), path);
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
