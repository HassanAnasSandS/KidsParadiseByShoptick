using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/youtube")]
public class AdminYouTubeController : ControllerBase
{
    private readonly IYouTubeAuthService _youTubeAuth;

    public AdminYouTubeController(IYouTubeAuthService youTubeAuth) => _youTubeAuth = youTubeAuth;

    [Authorize(Roles = "Admin")]
    [HttpGet("setup-info")]
    public ActionResult<object> GetSetupInfo([FromServices] IOptions<GoogleOAuthOptions> options)
    {
        var redirectUri = options.Value.RedirectUri;
        return Ok(new
        {
            clientId = options.Value.ClientId,
            redirectUri,
            googleConsoleNote =
                "Create a Web application OAuth client (not Installed/Android). " +
                "Add the redirectUri below exactly under Authorized redirect URIs.",
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
        => Ok(new { connected = _youTubeAuth.IsConnected });

    [Authorize(Roles = "Admin")]
    [HttpGet("auth-url")]
    public ActionResult<object> GetAuthUrl()
    {
        var url = _youTubeAuth.BuildAuthorizationUrl(out _);
        return Ok(new { url });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("access-token")]
    public async Task<ActionResult<object>> GetAccessToken(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _youTubeAuth.GetAccessTokenAsync(cancellationToken);
            return Ok(new { accessToken = token });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not connected", StringComparison.OrdinalIgnoreCase))
        {
            var authUrl = _youTubeAuth.BuildAuthorizationUrl(out _);
            return Unauthorized(new
            {
                message = ex.Message,
                needsAuth = true,
                authUrl,
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Content(
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>YouTube authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(error)}</p></body></html>",
                "text/html");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px'><h2>YouTube authorization failed</h2><p>Missing authorization code.</p></body></html>",
                "text/html");
        }

        try
        {
            await _youTubeAuth.CompleteAuthorizationAsync(state, code, cancellationToken);
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px;max-width:520px;margin:auto;text-align:center'>" +
                "<h2 style='color:#15803d'>YouTube connected</h2>" +
                "<p>Authorization complete. Return to the Kids Paradise Admin app and upload your toy video again.</p>" +
                "</body></html>",
                "text/html");
        }
        catch (Exception ex)
        {
            return Content(
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>YouTube authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p></body></html>",
                "text/html");
        }
    }
}
