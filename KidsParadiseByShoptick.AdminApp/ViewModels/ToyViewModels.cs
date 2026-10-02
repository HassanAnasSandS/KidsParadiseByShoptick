using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Config;
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
    [ObservableProperty] private string sortFilter = "Newest First";
    [ObservableProperty] private bool showFilters;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private bool isImageSearchActive;
    [ObservableProperty] private string? imageSearchHint;

    partial void OnIsImageSearchActiveChanged(bool value) => LoadMoreCommand.NotifyCanExecuteChanged();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<ToyListModel> Items { get; } = [];
    public ObservableCollection<string> CategoryOptions { get; } = ["All"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Available", "Sold"];
    public ObservableCollection<string> SaleOptions { get; } = ["All", "On Sale", "Regular"];
    public ObservableCollection<string> SortOptions { get; } =
        ["Newest First", "Name (A-Z)", "Price: Low to High", "Price: High to Low"];

    public bool HasActiveFilters =>
        IsImageSearchActive
        || !string.IsNullOrWhiteSpace(SearchText)
        || (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "All")
        || StatusFilter != "All"
        || SaleFilter != "All"
        || SortFilter != "Newest First";

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
            foreach (var c in CategoryNameSort.OrderByDisplayName(categories, x => x.Name))
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
            IsImageSearchActive = false;
            ImageSearchHint = null;
            Items.Clear();
            NotifyHasActiveFiltersChanged();
            LoadMoreCommand.NotifyCanExecuteChanged();
            await LoadNextPageCoreAsync(isRefresh: true);
        });
    }

    bool CanLoadMore() => !IsImageSearchActive && PagedListLoadCoordinator.CanLoadMore(HasMoreItems, IsBusy, IsLoadingMore, Items.Count);

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
            if (SortFilter != "Newest First")
                SortFilter = "Newest First";
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

    string? ResolveSort() => SortFilter switch
    {
        "Name (A-Z)" => "name",
        "Price: Low to High" => "price-low",
        "Price: High to Low" => "price-high",
        _ => null,
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
        SortFilter = "Newest First";
        IsImageSearchActive = false;
        ImageSearchHint = null;
        NotifyHasActiveFiltersChanged();
        await ReloadAsync();
    }

    [RelayCommand]
    async Task SearchByImageAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Search toys by image",
                FileTypes = FilePickerFileType.Images,
            });
            if (file is null) return;

            IsBusy = true;
            ErrorMessage = null;
            StatusText = "Searching by image…";

            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var matches = await _api.SearchToysByImageAsync(ms, file.FileName);
            Items.Clear();
            HasMoreItems = false;
            IsImageSearchActive = true;
            ImageSearchHint = matches.Count == 0
                ? "No matching toys. Tip: run “Index images” once for existing toys, or try a clearer photo."
                : $"Image search: {matches.Count} match(es). Exact = same upload; Similar = same toy, other photo.";

            foreach (var match in matches)
                Items.Add(match);

            StatusText = matches.Count == 0
                ? "No image matches"
                : $"Showing {matches.Count} image match(es)";
            NotifyHasActiveFiltersChanged();
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
        }
    }

    [RelayCommand]
    async Task IndexImagesAsync()
    {
        if (!await Shell.Current.DisplayAlert(
                "Index images",
                "Build free image fingerprints for all existing toys (perceptual hash + CLIP). First run may download a free ~87MB model. Continue?",
                "Index",
                "Cancel"))
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusText = "Indexing toy images…";
            var result = await _api.IndexToyImagesAsync();
            StatusText = result.Message;
            await Shell.Current.DisplayAlert("Index complete", result.Message, "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = string.Empty;
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            if (ex is UnauthorizedAccessException)
                await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
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
    async Task CloneAsync(ToyListModel item)
    {
        var soldNote = item.IsSold ? " It will be created as available." : string.Empty;
        if (!await Shell.Current.DisplayAlert(
                "Clone toy",
                $"Create a copy of \"{item.Name}\"?{soldNote}",
                "Clone",
                "Cancel"))
            return;

        try
        {
            IsBusy = true;
            await _api.CloneToyAsync(item.Id);
            await ReloadAsync();
            await Shell.Current.DisplayAlert("Cloned", $"\"{item.Name}\" was copied as a new available toy.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            if (ex is UnauthorizedAccessException)
                await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            IsBusy = false;
        }
    }

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
    private readonly IYouTubeUploadService _youTubeUpload;
    private readonly IMetaVideoUploadService _metaVideoUpload;
    private readonly ITikTokVideoUploadService _tikTokVideoUpload;
    private int? _id;
    /// <summary>Stable cache copy of the picked video (FileResult streams can go stale before Save).</summary>
    private string? _selectedVideoPath;
    private string _selectedVideoFileNameOnly = string.Empty;
    /// <summary>Previously saved server video (so Edit can re-post without re-selecting).</summary>
    private string? _storedVideoFilePath;
    private string? _storedVideoFileUrl;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string priceText = "0";
    [ObservableProperty] private string salePriceText = string.Empty;
    [ObservableProperty] private string videoLinkText = string.Empty;
    [ObservableProperty] private string selectedVideoFileName = string.Empty;
    [ObservableProperty] private string videoUploadStatus = string.Empty;
    [ObservableProperty] private bool isUploadingVideo;
    [ObservableProperty] private CategoryModel? selectedCategory;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool postToSocialMedia;
    [ObservableProperty] private string savingStatus = string.Empty;
    [ObservableProperty] private string title = "New Toy";

    public bool IsEditMode => _id.HasValue;

    public bool HasSelectedVideo => !string.IsNullOrWhiteSpace(_selectedVideoPath) && File.Exists(_selectedVideoPath);
    public bool HasStoredVideo => !string.IsNullOrWhiteSpace(_storedVideoFileUrl);
    public bool CanUploadVideo => _youTubeUpload.IsSupported && HasSelectedVideo && !IsUploadingVideo && !IsBusy;
    public bool CanSave => !IsBusy && !IsUploadingVideo;
    public bool CanPickVideo => !IsUploadingVideo && !IsBusy;

    public ObservableCollection<CategoryModel> Categories { get; } = [];
    public ObservableCollection<ToyImageItem> Images { get; } = [];

    public ToyEditViewModel(
        AdminApiService api,
        IYouTubeUploadService youTubeUpload,
        IMetaVideoUploadService metaVideoUpload,
        ITikTokVideoUploadService tikTokVideoUpload)
    {
        _api = api;
        _youTubeUpload = youTubeUpload;
        _metaVideoUpload = metaVideoUpload;
        _tikTokVideoUpload = tikTokVideoUpload;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
            _id = id;
        Title = _id.HasValue ? "Edit Toy" : "New Toy";
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        var categories = await _api.GetCategoriesAsync();
        Categories.Clear();
        foreach (var c in CategoryNameSort.OrderByDisplayName(categories, x => x.Name))
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
        VideoLinkText = toy.VideoLink ?? string.Empty;
        _storedVideoFilePath = toy.VideoFilePath;
        _storedVideoFileUrl = toy.VideoFileUrl;
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

        if (HasStoredVideo && !HasSelectedVideo)
        {
            SelectedVideoFileName = "Saved video on server (ready to re-post)";
            VideoUploadStatus = "Server already has this toy's video. Check social posting and Save — no need to select again.";
        }

        OnPropertyChanged(nameof(HasStoredVideo));
    }

    [RelayCommand]
    async Task PickImagesAsync()
    {
        var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Toy images",
            FileTypes = FilePickerFileType.Images,
        });
        if (files is null) return;

        var skipped = 0;
        foreach (var file in files)
        {
            // Some platform pickers ignore the type filter, so double-check the extension.
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".heic" or ".heif"))
            {
                skipped++;
                continue;
            }
            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            var result = await _api.UploadAsync(ms, file.FileName, "toys");
            Images.Add(new ToyImageItem { Path = result.Path, Url = result.Url });
        }

        if (skipped > 0)
        {
            await Shell.Current.DisplayAlert("Images only", $"{skipped} file(s) skipped. Please select images only; use \"Select video\" for videos.", "OK");
        }
    }

    [RelayCommand]
    void RemoveImage(ToyImageItem item) => Images.Remove(item);

    [RelayCommand]
    async Task PickVideoAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select toy video",
            FileTypes = FilePickerFileType.Videos,
        });

        if (file is null)
            return;

        try
        {
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".mp4";

            var cachePath = Path.Combine(
                FileSystem.CacheDirectory,
                $"toy-video-{Guid.NewGuid():N}{extension}");

            await using (var source = await file.OpenReadAsync())
            await using (var dest = File.Create(cachePath))
                await source.CopyToAsync(dest);

            if (new FileInfo(cachePath).Length <= 0)
            {
                try { File.Delete(cachePath); } catch { /* ignore */ }
                await Shell.Current.DisplayAlert("Video", "Selected video file is empty.", "OK");
                return;
            }

            DeleteCachedVideo();
            _selectedVideoPath = cachePath;
            _selectedVideoFileNameOnly = file.FileName;
            SelectedVideoFileName = file.FileName;
            VideoUploadStatus = _youTubeUpload.IsSupported
                ? "Video selected. On Save it uploads to YouTube (if no link yet) and posts to Facebook, Instagram & TikTok when social posting is on."
                : "Video selected. On Save it posts to Facebook, Instagram & TikTok when social posting is on.";
            OnPropertyChanged(nameof(HasSelectedVideo));
            OnPropertyChanged(nameof(CanUploadVideo));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Video", $"Could not load video: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    async Task UploadVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter toy name before uploading video.", "OK");
            return;
        }

        if (!HasSelectedVideo)
        {
            await Shell.Current.DisplayAlert("Video", "Please select a video first.", "OK");
            return;
        }

        if (!_youTubeUpload.IsSupported)
        {
            await Shell.Current.DisplayAlert("Video", "YouTube upload is not available.", "OK");
            return;
        }

        try
        {
            IsUploadingVideo = true;
            VideoUploadStatus = "Preparing upload…";
            OnPropertyChanged(nameof(CanUploadVideo));

            await using var stream = File.OpenRead(_selectedVideoPath!);
            var progress = new Progress<string>(status => VideoUploadStatus = status);

            VideoLinkText = await _youTubeUpload.UploadAsync(
                stream,
                _selectedVideoFileNameOnly,
                Name.Trim(),
                progress);

            VideoUploadStatus = "Uploaded to YouTube.";
        }
        catch (Exception ex)
        {
            VideoUploadStatus = string.Empty;
            await Shell.Current.DisplayAlert("YouTube upload failed", ex.Message, "OK");
        }
        finally
        {
            IsUploadingVideo = false;
            OnPropertyChanged(nameof(CanUploadVideo));
        }
    }

    [RelayCommand]
    void ClearVideo()
    {
        DeleteCachedVideo();
        SelectedVideoFileName = string.Empty;
        VideoUploadStatus = string.Empty;
        OnPropertyChanged(nameof(HasSelectedVideo));
        OnPropertyChanged(nameof(CanUploadVideo));
    }

    void DeleteCachedVideo()
    {
        if (!string.IsNullOrWhiteSpace(_selectedVideoPath))
        {
            try { File.Delete(_selectedVideoPath); } catch { /* ignore */ }
        }

        _selectedVideoPath = null;
        _selectedVideoFileNameOnly = string.Empty;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUploadVideo));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsUploadingVideoChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUploadVideo));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
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

        var willPostToSocial = !_id.HasValue || PostToSocialMedia;
        SocialPostActionsModel actions;
        try
        {
            var socialSettings = await _api.GetSocialMediaSettingsAsync();
            actions = (!_id.HasValue
                ? socialSettings.OnCreate
                : socialSettings.OnEdit) ?? new SocialPostActionsModel();
        }
        catch
        {
            actions = new SocialPostActionsModel();
        }

        var willPostPhotos = willPostToSocial && actions.HasAnyServerAction;
        var willPostMetaVideo = willPostToSocial && actions.MetaVideo;
        var willPostTikTokVideo = willPostToSocial && actions.TikTokVideo;
        var willUploadYouTube = actions.YouTube;

        if (willPostPhotos && Images.Count == 0)
        {
            await Shell.Current.DisplayAlert(
                "Validation",
                "At least one image is required for the enabled photo/catalog social actions.",
                "OK");
            return;
        }

        // Edit + social video: need a local pick OR a previously stored server video.
        if ((willPostMetaVideo || willPostTikTokVideo) && IsEditMode && !HasSelectedVideo && !HasStoredVideo)
        {
            var continuePhotosOnly = await Shell.Current.DisplayAlert(
                "No video on server",
                "Photos may still post.\n\nThis toy has no saved video file yet. Select a video once and Save — next time you can re-post without selecting again.",
                "Continue without video",
                "Cancel");
            if (!continuePhotosOnly)
                return;
        }

        try
        {
            IsBusy = true;

            // Persist video on Save: upload to YouTube if a file is selected and no link yet.
            if (HasSelectedVideo
                && string.IsNullOrWhiteSpace(VideoLinkText)
                && willUploadYouTube
                && _youTubeUpload.IsSupported)
            {
                try
                {
                    SavingStatus = "Uploading video to YouTube…";
                    VideoUploadStatus = "Uploading video to YouTube…";
                    await using var ytStream = File.OpenRead(_selectedVideoPath!);
                    var ytProgress = new Progress<string>(status =>
                    {
                        VideoUploadStatus = status;
                        SavingStatus = status;
                    });
                    VideoLinkText = await _youTubeUpload.UploadAsync(
                        ytStream,
                        _selectedVideoFileNameOnly,
                        Name.Trim(),
                        ytProgress);
                    VideoUploadStatus = "Uploaded to YouTube.";
                }
                catch (Exception ytEx)
                {
                    var continueWithoutYt = await Shell.Current.DisplayAlert(
                        "YouTube upload failed",
                        $"{ytEx.Message}\n\nContinue saving without a YouTube link?",
                        "Continue",
                        "Cancel");
                    if (!continueWithoutYt)
                        return;
                }
            }

            // Keep a copy on our server so Edit can re-post video later without re-selecting.
            string? videoFilePath = _storedVideoFilePath;
            if (HasSelectedVideo)
            {
                try
                {
                    SavingStatus = "Saving video to server…";
                    VideoUploadStatus = "Saving video to server…";
                    await using var uploadStream = File.OpenRead(_selectedVideoPath!);
                    var uploaded = await _api.UploadVideoAsync(uploadStream, _selectedVideoFileNameOnly);
                    videoFilePath = uploaded.Path;
                    _storedVideoFilePath = uploaded.Path;
                    _storedVideoFileUrl = uploaded.Url;
                    OnPropertyChanged(nameof(HasStoredVideo));
                }
                catch (Exception uploadEx)
                {
                    var continueWithoutServerVideo = await Shell.Current.DisplayAlert(
                        "Server video save failed",
                        $"{uploadEx.Message}\n\nContinue? (Social video can still post now, but Edit won't be able to re-post without selecting again.)",
                        "Continue",
                        "Cancel");
                    if (!continueWithoutServerVideo)
                        return;
                }
            }

            SavingStatus = "Saving toy…";
            var videoLink = string.IsNullOrWhiteSpace(VideoLinkText) ? null : VideoLinkText.Trim();
            if (_id.HasValue)
            {
                var updatePayload = new
                {
                    categoryId = SelectedCategory.Id,
                    name = Name.Trim(),
                    price,
                    salePrice,
                    videoLink,
                    videoFilePath,
                    imagePaths = Images.Select(i => i.Path).ToList(),
                    postToSocialMedia = PostToSocialMedia,
                };
                await _api.UpdateToyAsync(_id.Value, updatePayload);
            }
            else
            {
                var payload = new
                {
                    categoryId = SelectedCategory.Id,
                    name = Name.Trim(),
                    price,
                    salePrice,
                    videoLink,
                    videoFilePath,
                    imagePaths = Images.Select(i => i.Path).ToList(),
                };
                await _api.CreateToyAsync(payload);
            }

            if ((willPostMetaVideo || willPostTikTokVideo) && (HasSelectedVideo || HasStoredVideo))
            {
                var videoErrors = new List<string>();
                IsUploadingVideo = true;
                string? socialVideoPath = null;
                try
                {
                    socialVideoPath = await ResolveSocialVideoPathAsync();
                    if (string.IsNullOrWhiteSpace(socialVideoPath) || !File.Exists(socialVideoPath))
                    {
                        videoErrors.Add("Video: Could not load video file for social posting.");
                    }
                    else
                    {
                        var socialFileName = HasSelectedVideo
                            ? _selectedVideoFileNameOnly
                            : Path.GetFileName(socialVideoPath);

                        if (willPostMetaVideo)
                        {
                            try
                            {
                                SavingStatus = "Posting video to Facebook & Instagram…";
                                VideoUploadStatus = "Preparing Facebook/Instagram video upload…";

                                await using var stream = File.OpenRead(socialVideoPath);
                                var progress = new Progress<string>(status =>
                                {
                                    VideoUploadStatus = status;
                                    SavingStatus = status;
                                });

                                await _metaVideoUpload.UploadAsync(
                                    stream,
                                    socialFileName,
                                    Name.Trim(),
                                    price,
                                    salePrice,
                                    caption: null,
                                    progress);
                            }
                            catch (Exception metaEx)
                            {
                                videoErrors.Add($"Facebook/Instagram: {metaEx.Message}");
                            }
                        }

                        var tikTokPosted = false;
                        if (willPostTikTokVideo)
                        {
                            try
                            {
                                var tikTokStatus = await _api.GetTikTokStatusAsync();
                                if (tikTokStatus.Enabled && tikTokStatus.Connected)
                                {
                                    SavingStatus = "Posting video to TikTok…";
                                    VideoUploadStatus = "Preparing TikTok video upload…";

                                    await using var stream = File.OpenRead(socialVideoPath);
                                    var progress = new Progress<string>(status =>
                                    {
                                        VideoUploadStatus = status;
                                        SavingStatus = status;
                                    });

                                    await _tikTokVideoUpload.UploadAsync(
                                        stream,
                                        socialFileName,
                                        Name.Trim(),
                                        price,
                                        salePrice,
                                        caption: null,
                                        progress);
                                    tikTokPosted = true;
                                    if (string.Equals(tikTokStatus.PostMode, "MEDIA_UPLOAD", StringComparison.OrdinalIgnoreCase))
                                    {
                                        await Shell.Current.DisplayAlert(
                                            "TikTok video draft",
                                            "Video TikTok inbox mein chali gayi.\n\nTikTok video drafts pe caption API se nahi jaata — caption clipboard pe copy ho chuka hai. TikTok editor kholo aur paste karo.",
                                            "OK");
                                    }
                                }
                                else if (tikTokStatus.Enabled)
                                {
                                    VideoUploadStatus = "TikTok skipped (not connected).";
                                }
                            }
                            catch (Exception tikTokEx)
                            {
                                videoErrors.Add($"TikTok: {tikTokEx.Message}");
                            }
                        }

                        if (videoErrors.Count == 0)
                        {
                            VideoUploadStatus = (willPostMetaVideo, tikTokPosted) switch
                            {
                                (true, true) => "Video posted to Facebook, Instagram & TikTok.",
                                (true, false) => "Video posted to Facebook & Instagram.",
                                (false, true) => "Video posted to TikTok.",
                                _ => "Video posting complete.",
                            };
                        }
                    }

                    if (videoErrors.Count > 0)
                    {
                        VideoUploadStatus = string.Empty;
                        await Shell.Current.DisplayAlert(
                            "Video post incomplete",
                            $"Toy was saved (photos may still post in background).\n\n{string.Join("\n\n", videoErrors)}",
                            "OK");
                    }
                }
                finally
                {
                    IsUploadingVideo = false;
                    // Temp download from server — don't delete the user's selected cache until after.
                    if (!string.IsNullOrWhiteSpace(socialVideoPath)
                        && !string.Equals(socialVideoPath, _selectedVideoPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(socialVideoPath); } catch { /* ignore */ }
                    }
                }
            }

            DeleteCachedVideo();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            SavingStatus = string.Empty;
        }
    }

    /// <summary>Local picked file, or download the stored server video for social upload.</summary>
    async Task<string?> ResolveSocialVideoPathAsync()
    {
        if (HasSelectedVideo)
            return _selectedVideoPath;

        if (string.IsNullOrWhiteSpace(_storedVideoFileUrl))
            return null;

        SavingStatus = "Downloading saved video for social post…";
        VideoUploadStatus = "Downloading saved video…";

        var absoluteUrl = AppSettings.ResolveImageUrl(_storedVideoFileUrl);
        var extension = Path.GetExtension(_storedVideoFilePath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".mp4";

        var tempPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"toy-social-video-{Guid.NewGuid():N}{extension}");

        using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };
        await using var remote = await http.GetStreamAsync(absoluteUrl);
        await using var local = File.Create(tempPath);
        await remote.CopyToAsync(local);
        await local.FlushAsync();

        return new FileInfo(tempPath).Length > 0 ? tempPath : null;
    }
}

public class ToyImageItem
{
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
