using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class ReviewsViewModel : ObservableObject
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
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<ReviewModel> Items { get; } = [];

    public ReviewsViewModel(AdminApiService api) => _api = api;

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

            var result = await _api.GetReviewsPagedAsync(
                _load.CurrentPage,
                PageSize,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

            foreach (var item in result.Items)
                Items.Add(item);

            HasMoreItems = _load.CompletePage(Items.Count, result.TotalCount);
            StatusText = $"Showing {Items.Count} of {result.TotalCount} reviews";
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
    async Task EditAsync(ReviewModel item) => await Shell.Current.GoToAsync($"review-edit?id={item.Id}");
}

public partial class ReviewEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int _id;

    [ObservableProperty] private string reviewerName = string.Empty;
    [ObservableProperty] private int rating = 5;
    [ObservableProperty] private string comment = string.Empty;
    [ObservableProperty] private string? imagePath;
    [ObservableProperty] private string? imageUrl;
    [ObservableProperty] private string toyName = string.Empty;
    [ObservableProperty] private bool isBusy;

    public List<int> RatingOptions { get; } = [1, 2, 3, 4, 5];

    public ReviewEditViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj))
            int.TryParse(idObj?.ToString(), out _id);
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        var reviews = await _api.GetReviewsAsync();
        var review = reviews.FirstOrDefault(r => r.Id == _id);
        if (review is null) return;
        ReviewerName = review.ReviewerName;
        Rating = review.Rating;
        Comment = review.Comment;
        ImagePath = review.ImagePath;
        ImageUrl = review.ImageUrl;
        ToyName = review.ToyName;
    }

    [RelayCommand]
    async Task PickImageAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Review image" });
        if (file is null) return;
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;
        var result = await _api.UploadAsync(ms, file.FileName, "reviews");
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
        try
        {
            IsBusy = true;
            var payload = new Dictionary<string, object?>
            {
                ["reviewerName"] = ReviewerName.Trim(),
                ["rating"] = Rating,
                ["comment"] = Comment.Trim(),
            };
            if (!string.IsNullOrWhiteSpace(ImagePath))
                payload["imagePath"] = ImagePath;
            else if (string.IsNullOrWhiteSpace(ImageUrl))
                payload["imagePath"] = string.Empty;

            await _api.UpdateReviewAsync(_id, payload);
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

public partial class SiteImagesViewModel : ObservableObject
{
    private readonly AdminApiService _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isSavingDelivery;
    [ObservableProperty] private bool isSavingLinks;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string karachiChargeText = "300";
    [ObservableProperty] private string otherCitiesChargeText = "400";
    [ObservableProperty] private string whatsAppNumber = "923217175896";
    [ObservableProperty] private string whatsAppDisplay = "0321 7175896";
    [ObservableProperty] private string youTubeUrl = string.Empty;
    [ObservableProperty] private string facebookUrl = string.Empty;
    [ObservableProperty] private string instagramUrl = string.Empty;
    [ObservableProperty] private string tikTokUrl = string.Empty;
    [ObservableProperty] private string pinterestUrl = string.Empty;
    [ObservableProperty] private string selectedTab = "Delivery";

    public bool IsDeliveryTab => SelectedTab == "Delivery";
    public bool IsLinksTab => SelectedTab == "Links";
    public bool IsBrandTab => SelectedTab == "Brand";
    public bool IsHeroTab => SelectedTab == "Hero";
    public bool IsBannersTab => SelectedTab == "Banners";
    public bool IsPagesTab => SelectedTab == "Pages";
    public bool IsImagesTab => SelectedTab is "Brand" or "Hero" or "Banners" or "Pages";

    public ObservableCollection<SiteImageModel> Items { get; } = [];
    public ObservableCollection<SiteImageModel> FilteredItems { get; } = [];

    public SiteImagesViewModel(AdminApiService api) => _api = api;

    partial void OnSelectedTabChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsDeliveryTab));
        OnPropertyChanged(nameof(IsLinksTab));
        OnPropertyChanged(nameof(IsBrandTab));
        OnPropertyChanged(nameof(IsHeroTab));
        OnPropertyChanged(nameof(IsBannersTab));
        OnPropertyChanged(nameof(IsPagesTab));
        OnPropertyChanged(nameof(IsImagesTab));
        RefreshFilteredItems();
    }

    [RelayCommand]
    void SelectTab(string? tab)
    {
        if (tab is "Delivery" or "Links" or "Brand" or "Hero" or "Banners" or "Pages")
            SelectedTab = tab;
    }

    void RefreshFilteredItems()
    {
        var group = SelectedTab switch
        {
            "Brand" => "Brand",
            "Hero" => "Hero Slider",
            "Banners" => "Home Banners",
            "Pages" => "Pages",
            _ => null
        };

        FilteredItems.Clear();
        if (group is null) return;

        foreach (var img in Items.Where(x => x.Group == group).OrderBy(x => x.SortOrder))
            FilteredItems.Add(img);
    }

    [RelayCommand]
    async Task AppearingAsync() => await LoadAsync();

    [RelayCommand]
    async Task LoadAsync()
    {
        if (IsBusy) return;
        IsRefreshing = true;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var deliveryTask = _api.GetDeliveryChargesAsync();
            var linksTask = _api.GetSiteSocialLinksAsync();
            var imagesTask = _api.GetSiteImagesAsync();
            await Task.WhenAll(deliveryTask, linksTask, imagesTask);

            var delivery = await deliveryTask;
            KarachiChargeText = delivery.Karachi.ToString("0.##");
            OtherCitiesChargeText = delivery.OtherCities.ToString("0.##");

            var links = await linksTask;
            WhatsAppNumber = links.WhatsAppNumber;
            WhatsAppDisplay = links.WhatsAppDisplay;
            YouTubeUrl = links.YouTubeUrl;
            FacebookUrl = links.FacebookUrl;
            InstagramUrl = links.InstagramUrl;
            TikTokUrl = links.TikTokUrl;
            PinterestUrl = links.PinterestUrl;

            Items.Clear();
            foreach (var img in (await imagesTask).OrderBy(x => x.Group).ThenBy(x => x.SortOrder))
                Items.Add(img);
            RefreshFilteredItems();
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

    [RelayCommand]
    async Task SaveDeliveryChargesAsync()
    {
        if (!decimal.TryParse(KarachiChargeText.Trim(), out var karachi) || karachi < 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a valid Karachi delivery charge.", "OK");
            return;
        }

        if (!decimal.TryParse(OtherCitiesChargeText.Trim(), out var other) || other < 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a valid Other cities delivery charge.", "OK");
            return;
        }

        try
        {
            IsSavingDelivery = true;
            var saved = await _api.UpdateDeliveryChargesAsync(karachi, other);
            KarachiChargeText = saved.Karachi.ToString("0.##");
            OtherCitiesChargeText = saved.OtherCities.ToString("0.##");
            await Shell.Current.DisplayAlert(
                "Saved",
                "Delivery charges updated. Website checkout and order totals will use these rates.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsSavingDelivery = false;
        }
    }

    [RelayCommand]
    async Task SaveSocialLinksAsync()
    {
        if (string.IsNullOrWhiteSpace(WhatsAppNumber))
        {
            await Shell.Current.DisplayAlert("Validation", "WhatsApp number is required.", "OK");
            return;
        }

        try
        {
            IsSavingLinks = true;
            var saved = await _api.UpdateSiteSocialLinksAsync(new SiteSocialLinksModel
            {
                WhatsAppNumber = WhatsAppNumber.Trim(),
                WhatsAppDisplay = WhatsAppDisplay.Trim(),
                YouTubeUrl = YouTubeUrl.Trim(),
                FacebookUrl = FacebookUrl.Trim(),
                InstagramUrl = InstagramUrl.Trim(),
                TikTokUrl = TikTokUrl.Trim(),
                PinterestUrl = PinterestUrl.Trim(),
            });
            WhatsAppNumber = saved.WhatsAppNumber;
            WhatsAppDisplay = saved.WhatsAppDisplay;
            YouTubeUrl = saved.YouTubeUrl;
            FacebookUrl = saved.FacebookUrl;
            InstagramUrl = saved.InstagramUrl;
            TikTokUrl = saved.TikTokUrl;
            PinterestUrl = saved.PinterestUrl;
            await Shell.Current.DisplayAlert(
                "Saved",
                "Social links updated. Website footer, contact, and WhatsApp buttons will use these.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsSavingLinks = false;
        }
    }

    [RelayCommand]
    async Task UploadAsync(SiteImageModel item)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = item.Label });
        if (file is null) return;
        try
        {
            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            await _api.UploadSiteImageAsync(item.Key, ms, file.FileName);
            await LoadAsync();
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
    async Task ResetAsync(SiteImageModel item)
    {
        if (!await Shell.Current.DisplayAlert("Reset", $"Reset \"{item.Label}\" to default?", "Reset", "Cancel"))
            return;
        try
        {
            IsBusy = true;
            await _api.ResetSiteImageAsync(item.Key);
            await LoadAsync();
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
    async Task SaveContentAsync(SiteImageModel item)
    {
        if (item is null || !item.SupportsText)
            return;

        try
        {
            IsBusy = true;
            await _api.UpdateSiteImageContentAsync(
                item.Key,
                item.Title,
                item.Subtitle,
                item.CtaText,
                item.LinkUrl,
                item.TitleColor,
                item.SubtitleColor,
                item.CtaColor);
            await LoadAsync();
            await Shell.Current.DisplayAlert("Saved", $"Text updated for \"{item.Label}\".", "OK");
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

public partial class ShellViewModel : ObservableObject
{
    private readonly AdminApiService _api;
    private readonly OrderNotificationService _notifications;
    private readonly AuthSession _session;

    [ObservableProperty] private string username = "Admin";

    public ShellViewModel(AdminApiService api, OrderNotificationService notifications, AuthSession session)
    {
        _api = api;
        _notifications = notifications;
        _session = session;
        Username = session.Username ?? "Admin";
    }

    [RelayCommand]
    async Task LogoutAsync()
    {
        if (!await Shell.Current.DisplayAlert("Logout", "Sign out of admin panel?", "Logout", "Cancel"))
            return;
        _notifications.Stop();
        await _api.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
