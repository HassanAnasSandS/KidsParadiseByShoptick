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
    private readonly TikTokSocialOptions _options;

    public AdminTikTokController(ITikTokAuthService tikTokAuth, IOptions<TikTokSocialOptions> options)
    {
        _tikTokAuth = tikTokAuth;
        _options = options.Value;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
        => Ok(new
        {
            enabled = _options.Enabled,
            configured = _tikTokAuth.IsOAuthConfigured,
            connected = _tikTokAuth.IsConnected,
            postMode = _options.PostMode,
            privacyLevel = _options.PrivacyLevel,
        });

    [Authorize(Roles = "Admin")]
    [HttpGet("auth-url")]
    public ActionResult<object> GetAuthUrl()
    {
        try
        {
            var url = _tikTokAuth.BuildAuthorizationUrl(out _);
            return Ok(new { url });
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
            return Ok(new
            {
                accessToken,
                openId,
                postMode = _options.PostMode,
                privacyLevel = _options.PrivacyLevel,
            });
        }
        catch (InvalidOperationException ex)
        {
            string? authUrl = null;
            try
            {
                if (_tikTokAuth.IsOAuthConfigured)
                    authUrl = _tikTokAuth.BuildAuthorizationUrl(out _);
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
}
