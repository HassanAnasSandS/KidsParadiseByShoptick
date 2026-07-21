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
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string metaSummary = "Tap Check to verify Meta setup.";
    [ObservableProperty] private string catalogIdToLink = string.Empty;
    [ObservableProperty] private string tikTokSummary = "Tap Refresh to check TikTok connection.";
    [ObservableProperty] private bool tikTokConnected;
    [ObservableProperty] private bool tikTokConfigured;
    [ObservableProperty] private string pinterestSummary = "Tap Refresh to check Pinterest connection.";
    [ObservableProperty] private bool pinterestConnected;
    [ObservableProperty] private bool pinterestConfigured;
    [ObservableProperty] private string googleMerchantFeedUrl = "https://kidsparadise.shoptick.shop/google-merchant-feed.xml";

    public ObservableCollection<MetaRequirementCheckModel> MetaRequirements { get; } = [];

    public SocialMediaSettingsViewModel(AdminApiService api) => _api = api;

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
        await CheckMetaRequirementsAsync();
        await RefreshTikTokStatusAsync();
        await RefreshPinterestStatusAsync();
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var settings = await _api.GetSocialMediaSettingsAsync();
            Description = settings.Description;
            Tags = settings.Tags;
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

    [RelayCommand]
    async Task CheckMetaRequirementsAsync()
    {
        if (IsCheckingMeta) return;
        try
        {
            IsCheckingMeta = true;
            ErrorMessage = null;
            var status = await _api.GetMetaRequirementsAsync();
            MetaRequirements.Clear();
            foreach (var item in status.Requirements.OrderBy(r => r.Step))
                MetaRequirements.Add(item);

            CatalogIdToLink = status.WhatsAppCatalogId ?? CatalogIdToLink;
            MetaSummary = status.AllMet
                ? "All 4 Meta requirements are met."
                : $"{status.Requirements.Count(r => r.IsMet)}/{status.Requirements.Count} requirements met.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MetaSummary = "Could not check Meta requirements.";
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

            if (!status.Enabled)
                TikTokSummary = "TikTok is disabled on the server (TikTokSocial:Enabled=false).";
            else if (!status.Configured)
                TikTokSummary = "TikTok keys missing. Set ClientKey/ClientSecret in appsettings.Secrets.json.";
            else if (status.Connected)
                TikTokSummary = $"Connected. Mode: {status.PostMode} (photos via server, video from admin app).";
            else
                TikTokSummary = "Configured but not connected. Tap Connect TikTok.";
        }
        catch (Exception ex)
        {
            TikTokConfigured = false;
            TikTokConnected = false;
            TikTokSummary = $"Could not load TikTok status: {ex.Message}";
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
            var url = await _api.GetTikTokAuthUrlAsync();
            await Launcher.OpenAsync(new Uri(url));
            await Shell.Current.DisplayAlert(
                "TikTok",
                "Complete TikTok sign-in in the browser, then tap Refresh Status.",
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
        var confirm = await Shell.Current.DisplayAlert(
            "Disconnect TikTok?",
            "Toy posts will stop going to TikTok until you reconnect.",
            "Disconnect",
            "Cancel");
        if (!confirm) return;

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
                PinterestSummary = "Pinterest is disabled on the server (PinterestSocial:Enabled=false).";
            else if (!status.Configured)
                PinterestSummary = "Pinterest keys missing. Set AppId/AppSecret in appsettings.Secrets.json.";
            else if (status.Connected)
            {
                var board = string.IsNullOrWhiteSpace(status.BoardId)
                    ? status.DefaultBoardName ?? "Kids Paradise Toys"
                    : status.BoardId;
                PinterestSummary = $"Connected. Pins go to board: {board}.";
            }
            else
                PinterestSummary = "Configured but not connected. Tap Connect Pinterest.";
        }
        catch (Exception ex)
        {
            PinterestConfigured = false;
            PinterestConnected = false;
            PinterestSummary = $"Could not load Pinterest status: {ex.Message}";
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
                "Complete Pinterest sign-in in the browser, then tap Refresh Status.",
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
        var confirm = await Shell.Current.DisplayAlert(
            "Disconnect Pinterest?",
            "Toy posts will stop going to Pinterest until you reconnect.",
            "Disconnect",
            "Cancel");
        if (!confirm) return;

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
            var settings = await _api.UpdateSocialMediaSettingsAsync(Description, Tags);
            Description = settings.Description;
            Tags = settings.Tags;
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
