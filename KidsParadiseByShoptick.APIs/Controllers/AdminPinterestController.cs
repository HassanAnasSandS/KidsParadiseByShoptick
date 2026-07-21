using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/pinterest")]
public class AdminPinterestController : ControllerBase
{
    private readonly IPinterestAuthService _pinterestAuth;
    private readonly PinterestSocialOptions _options;

    public AdminPinterestController(IPinterestAuthService pinterestAuth, IOptions<PinterestSocialOptions> options)
    {
        _pinterestAuth = pinterestAuth;
        _options = options.Value;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus(CancellationToken cancellationToken)
    {
        var boardId = await _pinterestAuth.GetSavedBoardIdAsync(cancellationToken);
        return Ok(new
        {
            enabled = _options.Enabled,
            configured = _pinterestAuth.IsOAuthConfigured,
            connected = _pinterestAuth.IsConnected,
            boardId,
            defaultBoardName = _options.DefaultBoardName,
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("auth-url")]
    public ActionResult<object> GetAuthUrl()
    {
        try
        {
            var url = _pinterestAuth.BuildAuthorizationUrl(out _);
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message, configured = false });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("disconnect")]
    public async Task<ActionResult<object>> Disconnect(CancellationToken cancellationToken)
    {
        await _pinterestAuth.DisconnectAsync(cancellationToken);
        return Ok(new { message = "Pinterest disconnected." });
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
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>Pinterest authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(error_description ?? error)}</p></body></html>",
                "text/html");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px'><h2>Pinterest authorization failed</h2><p>Missing authorization code.</p></body></html>",
                "text/html");
        }

        try
        {
            await _pinterestAuth.CompleteAuthorizationAsync(state, code, cancellationToken);
            return Content(
                "<html><body style='font-family:sans-serif;padding:24px'>" +
                "<h2 style='color:#15803d'>Pinterest connected</h2>" +
                "<p>You can close this window and return to the Admin app.</p></body></html>",
                "text/html");
        }
        catch (Exception ex)
        {
            return Content(
                $"<html><body style='font-family:sans-serif;padding:24px'><h2>Pinterest authorization failed</h2><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p></body></html>",
                "text/html");
        }
    }
}
