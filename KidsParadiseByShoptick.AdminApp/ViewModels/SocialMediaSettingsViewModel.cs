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
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string metaSummary = "Tap Check to verify Meta setup.";
    [ObservableProperty] private string catalogIdToLink = string.Empty;

    public ObservableCollection<MetaRequirementCheckModel> MetaRequirements { get; } = [];

    public SocialMediaSettingsViewModel(AdminApiService api) => _api = api;

    [RelayCommand]
    async Task AppearingAsync()
    {
        await LoadAsync();
        await CheckMetaRequirementsAsync();
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
