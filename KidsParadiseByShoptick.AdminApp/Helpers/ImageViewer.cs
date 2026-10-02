using System.Windows.Input;
using KidsParadiseByShoptick.AdminApp.Config;

namespace KidsParadiseByShoptick.AdminApp.Helpers;

/// <summary>
/// Opens a full-screen image viewer. Use from XAML:
/// Command="{x:Static helpers:ImageViewer.OpenCommand}" CommandParameter="{Binding ImageUrl}"
/// </summary>
public static class ImageViewer
{
    public static ICommand OpenCommand { get; } = new Command<string?>(url => _ = OpenAsync(url));

    public static async Task OpenAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        var resolved = AppSettings.ResolveImageUrl(url.Trim());
        if (string.IsNullOrWhiteSpace(resolved))
            return;

        try
        {
            await Shell.Current.GoToAsync($"image-viewer?url={Uri.EscapeDataString(resolved)}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Image", ex.Message, "OK");
        }
    }
}
