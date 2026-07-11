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

    public AdminSocialHealthController(IYouTubeAuthService youTube, IMetaTokenService meta)
    {
        _youTube = youTube;
        _meta = meta;
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
        });
    }
}
