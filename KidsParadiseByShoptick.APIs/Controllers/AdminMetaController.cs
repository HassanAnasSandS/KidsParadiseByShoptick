using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/meta")]
public class AdminMetaController : ControllerBase
{
    private readonly IMetaTokenService _metaToken;

    public AdminMetaController(IMetaTokenService metaToken) => _metaToken = metaToken;

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
        => Ok(new { connected = _metaToken.IsConfigured });

    [Authorize(Roles = "Admin")]
    [HttpPost("connect")]
    public async Task<ActionResult<object>> Connect(
        [FromBody] MetaConnectRequest request,
        CancellationToken cancellationToken)
    {
        var credentials = await _metaToken.ConnectAsync(request.UserAccessToken, cancellationToken);
        return Ok(new
        {
            message = "Facebook and Instagram connected with a long-lived token.",
            facebookPageId = credentials.FacebookPageId,
            instagramBusinessAccountId = credentials.InstagramBusinessAccountId,
        });
    }
}

public record MetaConnectRequest(string UserAccessToken);
