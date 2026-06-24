using System.Text;
using System.Text.Json;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class AuthSession
{
    private const string TokenKey = "admin_token";
    private const string TokenBackupKey = "admin_token_backup";
    private const string RememberKey = "admin_remember";
    private const string UsernameKey = "admin_username";

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public bool RememberMe { get; private set; }

    public event Action? SessionChanged;

    public AuthSession()
    {
        RestoreFromPreferences();
    }

    public async Task LoadAsync()
    {
        RestoreFromPreferences();

        try
        {
            var secure = await SecureStorage.GetAsync(TokenKey);
            if (!string.IsNullOrEmpty(secure) && !IsTokenExpired(secure))
            {
                Token = secure;
                Preferences.Set(TokenBackupKey, secure);
            }
        }
        catch
        {
            // Preferences backup is the source of truth on Android.
        }

        if (!string.IsNullOrEmpty(Token) && IsTokenExpired(Token))
            await ClearAsync();
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && !IsTokenExpired(Token);

    public async Task SaveAsync(string token, string username, bool rememberMe)
    {
        Token = token;
        Username = username;
        RememberMe = rememberMe;
        WriteToken(token);
        Preferences.Set(RememberKey, rememberMe ? "1" : "0");
        Preferences.Set(UsernameKey, username);
        SessionChanged?.Invoke();
        await Task.CompletedTask;
    }

    public async Task ClearAsync()
    {
        Token = null;
        RemoveToken();
        Preferences.Set(OrderAlertListener.AlertsActiveKey, false);
        SessionChanged?.Invoke();
        await Task.CompletedTask;
    }

    public static string? GetStoredToken()
    {
        var pref = Preferences.Get(TokenBackupKey, null);
        if (!string.IsNullOrEmpty(pref))
            return pref;

        try
        {
            return SecureStorage.GetAsync(TokenKey).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    public static bool IsTokenExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return true;
            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("exp", out var expProp)) return true;
            var exp = expProp.GetInt64();
            // Small skew so a valid token is not cleared at the exact expiry second.
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp - 60;
        }
        catch
        {
            return false;
        }
    }

    private void RestoreFromPreferences()
    {
        RememberMe = Preferences.Get(RememberKey, "1") != "0";
        Username = Preferences.Get(UsernameKey, string.Empty);
        Token = Preferences.Get(TokenBackupKey, null);
        if (!string.IsNullOrEmpty(Token) && IsTokenExpired(Token))
            Token = null;
    }

    private static void WriteToken(string token)
    {
        Preferences.Set(TokenBackupKey, token);
        try
        {
            SecureStorage.SetAsync(TokenKey, token).GetAwaiter().GetResult();
        }
        catch
        {
            // Preferences backup is enough.
        }
    }

    private static void RemoveToken()
    {
        Preferences.Remove(TokenBackupKey);
        try
        {
            SecureStorage.Remove(TokenKey);
        }
        catch
        {
            // Ignore.
        }
    }

    private static string PadBase64(string value)
    {
        value = value.Replace('-', '+').Replace('_', '/');
        return value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');
    }
}
