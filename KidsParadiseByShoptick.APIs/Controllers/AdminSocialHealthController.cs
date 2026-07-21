using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/social-health")]
[Authorize(Roles = "Admin")]
public class AdminSocialHealthController : ControllerBase
{
    private readonly IYouTubeAuthService _youTube;
    private readonly IMetaTokenService _meta;
    private readonly ITikTokAuthService _tikTok;
    private readonly IPinterestAuthService _pinterest;

    public AdminSocialHealthController(
        IYouTubeAuthService youTube,
        IMetaTokenService meta,
        ITikTokAuthService tikTok,
        IPinterestAuthService pinterest)
    {
        _youTube = youTube;
        _meta = meta;
        _tikTok = tikTok;
        _pinterest = pinterest;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetStatus(CancellationToken cancellationToken)
    {
        var youtubeOk = false;
        string? youtubeError = null;
        if (_youTube.IsConnected)
        {
            try
            {
                await _youTube.GetAccessTokenAsync(cancellationToken);
                youtubeOk = true;
            }
            catch (Exception ex)
            {
                youtubeError = ex.Message;
            }
        }

        var metaOk = false;
        string? metaError = null;
        if (_meta.IsConfigured)
        {
            try
            {
                await _meta.EnsureCredentialsAsync(cancellationToken);
                metaOk = true;
            }
            catch (Exception ex)
            {
                metaError = ex.Message;
            }
        }

        var tikTokOk = false;
        string? tikTokError = null;
        if (_tikTok.IsConnected)
        {
            try
            {
                await _tikTok.GetAccessTokenAsync(cancellationToken);
                tikTokOk = true;
            }
            catch (Exception ex)
            {
                tikTokError = ex.Message;
            }
        }

        var pinterestOk = false;
        string? pinterestError = null;
        if (_pinterest.IsConnected)
        {
            try
            {
                await _pinterest.GetAccessTokenAsync(cancellationToken);
                pinterestOk = true;
            }
            catch (Exception ex)
            {
                pinterestError = ex.Message;
            }
        }

        return Ok(new
        {
            youtube = new
            {
                configured = _youTube.IsOAuthConfigured,
                connected = _youTube.IsConnected,
                healthy = youtubeOk,
                error = youtubeError,
            },
            meta = new
            {
                connected = _meta.IsConfigured,
                healthy = metaOk,
                error = metaError,
            },
            tiktok = new
            {
                configured = _tikTok.IsOAuthConfigured,
                connected = _tikTok.IsConnected,
                healthy = tikTokOk,
                error = tikTokError,
            },
            pinterest = new
            {
                configured = _pinterest.IsOAuthConfigured,
                connected = _pinterest.IsConnected,
                healthy = pinterestOk,
                error = pinterestError,
            },
        });
    }
}
