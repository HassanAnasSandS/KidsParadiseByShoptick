using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/tiktok")]
public class AdminTikTokController : ControllerBase
{
    private readonly ITikTokAuthService _tikTokAuth;
    private readonly ITikTokSocialService _tikTokSocial;
    private readonly ISocialMediaSettingsService _socialSettings;
    private readonly TikTokSocialOptions _options;

    public AdminTikTokController(
        ITikTokAuthService tikTokAuth,
        ITikTokSocialService tikTokSocial,
        ISocialMediaSettingsService socialSettings,
        IOptions<TikTokSocialOptions> options)
    {
        _tikTokAuth = tikTokAuth;
        _tikTokSocial = tikTokSocial;
        _socialSettings = socialSettings;
        _options = options.Value;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus(CancellationToken cancellationToken)
    {
        var postMode = await _socialSettings.GetTikTokPostModeAsync(cancellationToken);
        return Ok(new
        {
            enabled = _options.Enabled,
            configured = _tikTokAuth.IsOAuthConfigured,
            connected = _tikTokAuth.IsConnected,
            postMode,
            privacyLevel = PrivacyForMode(postMode),
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("post-mode")]
    public async Task<ActionResult<object>> SetPostMode(
        [FromBody] UpdateTikTokPostModeRequest request,
        CancellationToken cancellationToken)
    {
        var postMode = await _socialSettings.SetTikTokPostModeAsync(request.PostMode, cancellationToken);
        return Ok(new
        {
            enabled = _options.Enabled,
            configured = _tikTokAuth.IsOAuthConfigured,
            connected = _tikTokAuth.IsConnected,
            postMode,
            privacyLevel = PrivacyForMode(postMode),
            needsReconnect = _tikTokAuth.IsConnected,
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("auth-url")]
    public async Task<ActionResult<object>> GetAuthUrl(
        [FromQuery] string? postMode,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(postMode))
                await _socialSettings.SetTikTokPostModeAsync(postMode, cancellationToken);

            var mode = await _socialSettings.GetTikTokPostModeAsync(cancellationToken);
            var url = _tikTokAuth.BuildAuthorizationUrl(mode, out _);
            return Ok(new { url, postMode = mode });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message, configured = false });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("access-token")]
    public async Task<ActionResult<object>> GetAccessToken(CancellationToken cancellationToken)
    {
        try
        {
            var (accessToken, openId) = await _tikTokAuth.GetAccessTokenAsync(cancellationToken);
            var postMode = await _socialSettings.GetTikTokPostModeAsync(cancellationToken);
            var privacyLevel = TikTokSocialOptions.IsDraft(postMode)
                ? (string.IsNullOrWhiteSpace(_options.PrivacyLevel) ? "SELF_ONLY" : _options.PrivacyLevel)
                : await _tikTokSocial.ResolvePrivacyLevelAsync(accessToken, cancellationToken);

            return Ok(new
            {
                accessToken,
                openId,
                postMode,
                privacyLevel,
            });
        }
        catch (InvalidOperationException ex)
        {
            string? authUrl = null;
            try
            {
                if (_tikTokAuth.IsOAuthConfigured)
                {
                    var mode = await _socialSettings.GetTikTokPostModeAsync(cancellationToken);
                    authUrl = _tikTokAuth.BuildAuthorizationUrl(mode, out _);
                }
            }
            catch
            {
                // ignore
            }

            return Unauthorized(new
            {
                message = ex.Message,
                needsAuth = true,
                authUrl,
            });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("post-photos/{toyId:int}")]
    public async Task<ActionResult<object>> PostPhotos(int toyId, CancellationToken cancellationToken)
    {
        try
        {
            var publishId = await _tikTokSocial.PostToyPhotosAsync(toyId, cancellationToken);
            if (string.IsNullOrWhiteSpace(publishId))
                return BadRequest(new { message = "TikTok is not connected or photos were skipped." });
            return Ok(new { publishId, message = "TikTok photo draft/post started." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("disconnect")]
    public async Task<ActionResult<object>> Disconnect(CancellationToken cancellationToken)
    {
        await _tikTokAuth.DisconnectAsync(cancellationToken);
        return Ok(new { message = "TikTok disconnected." });
    }

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<ContentResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Content(
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>TikTok authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(error_description ?? error)}</p></body></html>",
                "text/html");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px'><h2>TikTok authorization failed</h2><p>Missing authorization code.</p></body></html>",
                "text/html");
        }

        try
        {
            await _tikTokAuth.CompleteAuthorizationAsync(state, code, cancellationToken);
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px'>" +
                "<h2 style='color:#15803d'>TikTok connected</h2>" +
                "<p>You can close this window and return to the Admin app.</p></body></html>",
                "text/html");
        }
        catch (Exception ex)
        {
            return Content(
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>TikTok authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p></body></html>",
                "text/html");
        }
    }

    string PrivacyForMode(string postMode) =>
        string.IsNullOrWhiteSpace(_options.PrivacyLevel)
            ? (TikTokSocialOptions.IsDraft(postMode) ? "SELF_ONLY" : "PUBLIC_TO_EVERYONE")
            : _options.PrivacyLevel;
}
