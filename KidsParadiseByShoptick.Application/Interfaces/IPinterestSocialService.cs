namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IPinterestSocialService
{
    bool IsOAuthConfigured { get; }

    /// <summary>OAuth configured and a Pinterest account is connected.</summary>
    bool IsConfigured { get; }

    /// <summary>Creates a Pin for the toy on the connected board. Returns pin id.</summary>
    Task<string?> PostToyPinAsync(int toyId, CancellationToken cancellationToken = default);
}
