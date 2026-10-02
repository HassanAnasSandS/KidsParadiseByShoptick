using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class SocialMediaSettingsViewModel : ObservableObject
{
    private readonly AdminApiService _api;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isCheckingMeta;
    [ObservableProperty] private bool isCheckingTikTok;
    [ObservableProperty] private bool isCheckingPinterest;
    [ObservableProperty] private bool isCheckingYouTube;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private bool metaConnected;
    [ObservableProperty] private string metaStatusLabel = "Checking…";
    [ObservableProperty] private string metaSummary = "Facebook, Instagram & WhatsApp catalog.";
    [ObservableProperty] private string metaUserAccessToken = string.Empty;
    [ObservableProperty] private string catalogIdToLink = string.Empty;
    [ObservableProperty] private bool showMetaConnectForm;

    [ObservableProperty] private string tikTokSummary = "Checking…";
    [ObservableProperty] private string tikTokStatusLabel = "Checking…";
    [ObservableProperty] private bool tikTokConnected;
    [ObservableProperty] private bool tikTokConfigured;
    [ObservableProperty] private string tikTokPostMode = "DIRECT_POST";
    [ObservableProperty] private bool isTikTokDraftMode;
    [ObservableProperty] private bool isTikTokDirectPostMode = true;

    [ObservableProperty] private string pinterestSummary = "Checking…";
    [ObservableProperty] private string pinterestStatusLabel = "Checking…";
    [ObservableProperty] private bool pinterestConnected;
    [ObservableProperty] private bool pinterestConfigured;

    [ObservableProperty] private string youTubeSummary = "Checking…";
    [ObservableProperty] private string youTubeStatusLabel = "Checking…";
    [ObservableProperty] private bool youTubeConnected;
    [ObservableProperty] private bool youTubeConfigured;

    [ObservableProperty] private string googleMerchantFeedUrl = "https://kidsparadise.shoptick.shop/google-merchant-feed.xml";
    [ObservableProperty] private string selectedTab = "Accounts";
    [ObservableProperty] private string selectedAccountsSubTab = "Meta";
    [ObservableProperty] private string selectedPostingSubTab = "Create";
    [ObservableProperty] private string selectedCaptionSubTab = "Description";
    [ObservableProperty] private SocialPostActionsModel onCreate = new();
    [ObservableProperty] private SocialPostActionsModel onEdit = new();

    public bool IsAccountsTab => SelectedTab == "Accounts";
    public bool IsPostingTab => SelectedTab == "Posting";
    public bool IsCaptionTab => SelectedTab == "Caption";

    public bool IsMetaSubTab => SelectedAccountsSubTab == "Meta";
    public bool IsTikTokSubTab => SelectedAccountsSubTab == "TikTok";
    public bool IsPinterestSubTab => SelectedAccountsSubTab == "Pinterest";
    public bool IsYouTubeSubTab => SelectedAccountsSubTab == "YouTube";
    public bool IsGoogleSubTab => SelectedAccountsSubTab == "Google";

    public bool IsCreatePostingSubTab => SelectedPostingSubTab == "Create";
    public bool IsEditPostingSubTab => SelectedPostingSubTab == "Edit";

    public bool IsDescriptionCaptionSubTab => SelectedCaptionSubTab == "Description";
    public bool IsTagsCaptionSubTab => SelectedCaptionSubTab == "Tags";

    public ObservableCollection<MetaRequirementCheckModel> MetaRequirements { get; } = [];

    public SocialMediaSettingsViewModel(AdminApiService api) => _api = api;

    partial void OnSelectedTabChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsAccountsTab));
        OnPropertyChanged(nameof(IsPostingTab));
        OnPropertyChanged(nameof(IsCaptionTab));
    }

    partial void OnSelectedAccountsSubTabChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsMetaSubTab));
        OnPropertyChanged(nameof(IsTikTokSubTab));
        OnPropertyChanged(nameof(IsPinterestSubTab));
        OnPropertyChanged(nameof(IsYouTubeSubTab));
        OnPropertyChanged(nameof(IsGoogleSubTab));
    }

    partial void OnSelectedPostingSubTabChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsCreatePostingSubTab));
        OnPropertyChanged(nameof(IsEditPostingSubTab));
    }

    partial void OnSelectedCaptionSubTabChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsDescriptionCaptionSubTab));
        OnPropertyChanged(nameof(IsTagsCaptionSubTab));
    }

    [RelayCommand]
    void SelectTab(string? tab)
    {
        if (tab is "Accounts" or "Posting" or "Caption")
            SelectedTab = tab;
    }

    [RelayCommand]
    void SelectAccountsSubTab(string? tab)
    {
        if (tab is "Meta" or "TikTok" or "Pinterest" or "YouTube" or "Google")
            SelectedAccountsSubTab = tab;
    }

    [RelayCommand]
    void SelectPostingSubTab(string? tab)
    {
        if (tab is "Create" or "Edit")
            SelectedPostingSubTab = tab;
    }

    [RelayCommand]
    void SelectCaptionSubTab(string? tab)
    {
        if (tab is "Description" or "Tags")
            SelectedCaptionSubTab = tab;
    }

    [RelayCommand]
    void ToggleMetaConnectForm() => ShowMetaConnectForm = !ShowMetaConnectForm;

    [RelayCommand]
    async Task CopyGoogleMerchantFeedUrlAsync()
    {
        await Clipboard.Default.SetTextAsync(GoogleMerchantFeedUrl);
        await Shell.Current.DisplayAlert("Copied", "Google Merchant feed URL copied.", "OK");
    }

    [RelayCommand]
    async Task OpenGoogleMerchantFeedAsync()
    {
        try
        {
            await Browser.Default.OpenAsync(GoogleMerchantFeedUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Open failed", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        await LoadAsync();
        await RefreshAllAccountsAsync();
    }

    [RelayCommand]
    async Task RefreshAllAccountsAsync()
    {
        await Task.WhenAll(
            CheckMetaRequirementsAsync(),
            RefreshTikTokStatusAsync(),
            RefreshPinterestStatusAsync(),
            RefreshYouTubeStatusAsync());
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ApplySettings(await _api.GetSocialMediaSettingsAsync());
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
        }
    }

    void ApplySettings(SocialMediaSettingsModel settings)
    {
        Description = settings.Description;
        Tags = settings.Tags;
        ApplyTikTokPostMode(settings.TikTokPostMode);
        OnCreate = settings.OnCreate ?? new SocialPostActionsModel();
        OnEdit = settings.OnEdit ?? new SocialPostActionsModel();
    }

    [RelayCommand]
    async Task CheckMetaRequirementsAsync()
    {
        if (IsCheckingMeta) return;
        try
        {
            IsCheckingMeta = true;
            ErrorMessage = null;

            var statusTask = _api.GetMetaStatusAsync();
            var requirementsTask = _api.GetMetaRequirementsAsync();
            await Task.WhenAll(statusTask, requirementsTask);

            var connection = await statusTask;
            var status = await requirementsTask;

            MetaConnected = connection.Connected;
            MetaRequirements.Clear();
            foreach (var item in status.Requirements.OrderBy(r => r.Step))
                MetaRequirements.Add(item);

            CatalogIdToLink = status.WhatsAppCatalogId ?? CatalogIdToLink;

            if (!connection.Connected)
            {
                MetaStatusLabel = "Not connected";
                MetaSummary = "Connect Facebook Page + Instagram + WhatsApp catalog with a Meta user token.";
            }
            else if (status.AllMet)
            {
                MetaStatusLabel = "Connected";
                MetaSummary = string.IsNullOrWhiteSpace(status.FacebookPageId)
                    ? "All Meta requirements are met."
                    : $"Ready · Page {status.FacebookPageId}";
            }
            else
            {
                MetaStatusLabel = "Needs setup";
                MetaSummary = $"{status.Requirements.Count(r => r.IsMet)}/{status.Requirements.Count} requirements met.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MetaConnected = false;
            MetaStatusLabel = "Error";
            MetaSummary = "Could not check Meta status.";
        }
        finally
        {
            IsCheckingMeta = false;
        }
    }

    [RelayCommand]
    async Task ConnectMetaAsync()
    {
        if (string.IsNullOrWhiteSpace(MetaUserAccessToken))
        {
            await Shell.Current.DisplayAlert(
                "Token required",
                "Paste a Meta User Access Token from Graph API Explorer (with page + Instagram + WhatsApp permissions).",
                "OK");
            ShowMetaConnectForm = true;
            return;
        }

        try
        {
            IsCheckingMeta = true;
            await _api.ConnectMetaAsync(
                MetaUserAccessToken.Trim(),
                facebookPageId: null,
                whatsAppCatalogId: string.IsNullOrWhiteSpace(CatalogIdToLink) ? null : CatalogIdToLink.Trim());
            MetaUserAccessToken = string.Empty;
            ShowMetaConnectForm = false;
            await CheckMetaRequirementsAsync();
            await Shell.Current.DisplayAlert("Connected", "Facebook / Instagram / WhatsApp catalog connected.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Meta connect failed", ex.Message, "OK");
        }
        finally
        {
            IsCheckingMeta = false;
        }
    }

    [RelayCommand]
    async Task DisconnectMetaAsync()
    {
        if (!await Shell.Current.DisplayAlert(
                "Disconnect Meta?",
                "Facebook, Instagram and WhatsApp catalog posting will stop until you reconnect.",
                "Disconnect",
                "Cancel"))
            return;

        try
        {
            IsCheckingMeta = true;
            await _api.DisconnectMetaAsync();
            await CheckMetaRequirementsAsync();
            await Shell.Current.DisplayAlert("Disconnected", "Meta account disconnected.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsCheckingMeta = false;
        }
    }

    [RelayCommand]
    async Task RefreshTikTokStatusAsync()
    {
        if (IsCheckingTikTok) return;
        try
        {
            IsCheckingTikTok = true;
            var status = await _api.GetTikTokStatusAsync();
            TikTokConfigured = status.Configured;
            TikTokConnected = status.Connected;
            ApplyTikTokPostMode(status.PostMode);

            if (!status.Enabled)
            {
                TikTokStatusLabel = "Disabled";
                TikTokSummary = "TikTok is disabled on the server.";
            }
            else if (!status.Configured)
            {
                TikTokStatusLabel = "Setup needed";
                TikTokSummary = "Add ClientKey / ClientSecret in server secrets.";
            }
            else if (status.Connected)
            {
                TikTokStatusLabel = "Connected";
                TikTokSummary = IsTikTokDraftMode
                    ? "Draft mode · posts go to TikTok inbox."
                    : "Direct Post · publishes immediately.";
            }
            else
            {
                TikTokStatusLabel = "Not connected";
                TikTokSummary = "Choose Draft or Direct Post, then connect.";
            }
        }
        catch (Exception ex)
        {
            TikTokConfigured = false;
            TikTokConnected = false;
            TikTokStatusLabel = "Error";
            TikTokSummary = ex.Message;
        }
        finally
        {
            IsCheckingTikTok = false;
        }
    }

    [RelayCommand]
    async Task ConnectTikTokAsync()
    {
        try
        {
            IsCheckingTikTok = true;
            var url = await _api.GetTikTokAuthUrlAsync(TikTokPostMode);
            await Launcher.OpenAsync(new Uri(url));
            await Shell.Current.DisplayAlert(
                "TikTok",
                "Finish sign-in in the browser, then tap Refresh.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("TikTok connect failed", ex.Message, "OK");
        }
        finally
        {
            IsCheckingTikTok = false;
        }
    }

    [RelayCommand]
    async Task DisconnectTikTokAsync()
    {
        if (!await Shell.Current.DisplayAlert(
                "Disconnect TikTok?",
                "TikTok posting will stop until you reconnect.",
                "Disconnect",
                "Cancel"))
            return;

        try
        {
            IsCheckingTikTok = true;
            await _api.DisconnectTikTokAsync();
            await RefreshTikTokStatusAsync();
            await Shell.Current.DisplayAlert("Disconnected", "TikTok account disconnected.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsCheckingTikTok = false;
        }
    }

    [RelayCommand]
    async Task RefreshPinterestStatusAsync()
    {
        if (IsCheckingPinterest) return;
        try
        {
            IsCheckingPinterest = true;
            var status = await _api.GetPinterestStatusAsync();
            PinterestConfigured = status.Configured;
            PinterestConnected = status.Connected;

            if (!status.Enabled)
            {
                PinterestStatusLabel = "Disabled";
                PinterestSummary = "Pinterest is disabled on the server.";
            }
            else if (!status.Configured)
            {
                PinterestStatusLabel = "Setup needed";
                PinterestSummary = "Add AppId / AppSecret in server secrets.";
            }
            else if (status.Connected)
            {
                PinterestStatusLabel = "Connected";
                var board = string.IsNullOrWhiteSpace(status.BoardId)
                    ? status.DefaultBoardName ?? "default board"
                    : status.BoardId;
                PinterestSummary = $"Pins go to: {board}";
            }
            else
            {
                PinterestStatusLabel = "Not connected";
                PinterestSummary = "Tap Connect to authorize Pinterest.";
            }
        }
        catch (Exception ex)
        {
            PinterestConfigured = false;
            PinterestConnected = false;
            PinterestStatusLabel = "Error";
            PinterestSummary = ex.Message;
        }
        finally
        {
            IsCheckingPinterest = false;
        }
    }

    [RelayCommand]
    async Task ConnectPinterestAsync()
    {
        try
        {
            IsCheckingPinterest = true;
            var url = await _api.GetPinterestAuthUrlAsync();
            await Launcher.OpenAsync(new Uri(url));
            await Shell.Current.DisplayAlert(
                "Pinterest",
                "Finish sign-in in the browser, then tap Refresh.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Pinterest connect failed", ex.Message, "OK");
        }
        finally
        {
            IsCheckingPinterest = false;
        }
    }

    [RelayCommand]
    async Task DisconnectPinterestAsync()
    {
        if (!await Shell.Current.DisplayAlert(
                "Disconnect Pinterest?",
                "Pinterest pinning will stop until you reconnect.",
                "Disconnect",
                "Cancel"))
            return;

        try
        {
            IsCheckingPinterest = true;
            await _api.DisconnectPinterestAsync();
            await RefreshPinterestStatusAsync();
            await Shell.Current.DisplayAlert("Disconnected", "Pinterest account disconnected.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsCheckingPinterest = false;
        }
    }

    [RelayCommand]
    async Task RefreshYouTubeStatusAsync()
    {
        if (IsCheckingYouTube) return;
        try
        {
            IsCheckingYouTube = true;
            var status = await _api.GetYouTubeStatusAsync();
            YouTubeConfigured = status.Configured;
            YouTubeConnected = status.Connected;

            if (!status.Configured)
            {
                YouTubeStatusLabel = "Setup needed";
                YouTubeSummary = "Add Google OAuth ClientId / RedirectUri on the server.";
            }
            else if (status.Connected)
            {
                YouTubeStatusLabel = "Connected";
                YouTubeSummary = "Ready for video uploads from the admin app.";
            }
            else
            {
                YouTubeStatusLabel = "Not connected";
                YouTubeSummary = "Tap Connect to authorize YouTube upload.";
            }
        }
        catch (Exception ex)
        {
            YouTubeConfigured = false;
            YouTubeConnected = false;
            YouTubeStatusLabel = "Error";
            YouTubeSummary = ex.Message;
        }
        finally
        {
            IsCheckingYouTube = false;
        }
    }

    [RelayCommand]
    async Task ConnectYouTubeAsync()
    {
        try
        {
            IsCheckingYouTube = true;
            var url = await _api.GetYouTubeAuthUrlAsync();
            await Launcher.OpenAsync(new Uri(url));
            await Shell.Current.DisplayAlert(
                "YouTube",
                "Finish Google sign-in in the browser, then tap Refresh.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("YouTube connect failed", ex.Message, "OK");
        }
        finally
        {
            IsCheckingYouTube = false;
        }
    }

    [RelayCommand]
    async Task DisconnectYouTubeAsync()
    {
        if (!await Shell.Current.DisplayAlert(
                "Disconnect YouTube?",
                "YouTube uploads will stop until you reconnect.",
                "Disconnect",
                "Cancel"))
            return;

        try
        {
            IsCheckingYouTube = true;
            await _api.DisconnectYouTubeAsync();
            await RefreshYouTubeStatusAsync();
            await Shell.Current.DisplayAlert("Disconnected", "YouTube account disconnected.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsCheckingYouTube = false;
        }
    }

    [RelayCommand]
    async Task LinkCatalogAsync()
    {
        if (string.IsNullOrWhiteSpace(CatalogIdToLink))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter your Commerce Manager Catalog ID.", "OK");
            return;
        }

        try
        {
            IsCheckingMeta = true;
            await _api.LinkWhatsAppCatalogAsync(CatalogIdToLink.Trim());
            await CheckMetaRequirementsAsync();
            await Shell.Current.DisplayAlert("Linked", "Catalog linked to WhatsApp Business Account.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsCheckingMeta = false;
        }
    }

    [RelayCommand]
    async Task SelectTikTokPostModeAsync(string? mode)
    {
        var next = string.Equals(mode, "MEDIA_UPLOAD", StringComparison.OrdinalIgnoreCase)
            ? "MEDIA_UPLOAD"
            : "DIRECT_POST";
        if (string.Equals(next, TikTokPostMode, StringComparison.OrdinalIgnoreCase)
            && ((next == "MEDIA_UPLOAD") == IsTikTokDraftMode))
            return;

        var wasConnected = TikTokConnected;
        ApplyTikTokPostMode(next);

        try
        {
            IsCheckingTikTok = true;
            var status = await _api.UpdateTikTokPostModeAsync(next);
            TikTokConfigured = status.Configured;
            TikTokConnected = status.Connected;
            ApplyTikTokPostMode(status.PostMode);
            await RefreshTikTokStatusAsync();

            if (wasConnected || status.NeedsReconnect)
            {
                await Shell.Current.DisplayAlert(
                    "Reconnect TikTok",
                    "Posting mode changed. Disconnect and Connect again so TikTok grants the matching permission.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("TikTok mode failed", ex.Message, "OK");
        }
        finally
        {
            IsCheckingTikTok = false;
        }
    }

    void ApplyTikTokPostMode(string? mode)
    {
        TikTokPostMode = string.Equals(mode, "MEDIA_UPLOAD", StringComparison.OrdinalIgnoreCase)
            ? "MEDIA_UPLOAD"
            : "DIRECT_POST";
        IsTikTokDraftMode = TikTokPostMode == "MEDIA_UPLOAD";
        IsTikTokDirectPostMode = !IsTikTokDraftMode;
    }

    [RelayCommand]
    async Task ShowFixAsync(MetaRequirementCheckModel? item)
    {
        if (item is null) return;
        await Shell.Current.DisplayAlert($"Step {item.Step}: {item.Title}", item.FixInstructions, "OK");
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var payload = new SocialMediaSettingsModel
            {
                Description = Description,
                Tags = Tags,
                TikTokPostMode = TikTokPostMode,
                OnCreate = OnCreate,
                OnEdit = OnEdit,
            };
            ApplySettings(await _api.UpdateSocialMediaSettingsAsync(payload));
            await Shell.Current.DisplayAlert("Saved", "Social media settings updated.", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
