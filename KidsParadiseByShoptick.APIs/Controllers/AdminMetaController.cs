using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/meta")]
public class AdminMetaController : ControllerBase
{
    private readonly IMetaTokenService _metaToken;
    private readonly IMetaRequirementsService _requirements;

    public AdminMetaController(IMetaTokenService metaToken, IMetaRequirementsService requirements)
    {
        _metaToken = metaToken;
        _requirements = requirements;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
        => Ok(new { connected = _metaToken.IsConfigured });

    /// <summary>
    /// Page token + IDs for Admin app direct Facebook/Instagram video upload (video bytes never hit this server).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("upload-credentials")]
    public async Task<ActionResult<object>> GetUploadCredentials(CancellationToken cancellationToken)
    {
        if (!_metaToken.IsConfigured)
            return BadRequest(new { message = "Facebook/Instagram is not connected on the server." });

        try
        {
            var credentials = await _metaToken.EnsureCredentialsAsync(cancellationToken);
            return Ok(new
            {
                facebookPageId = credentials.FacebookPageId,
                pageAccessToken = credentials.PageAccessToken,
                instagramBusinessAccountId = credentials.InstagramBusinessAccountId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("requirements")]
    public async Task<ActionResult<MetaRequirementsStatusDto>> GetRequirements(CancellationToken cancellationToken)
        => Ok(await _requirements.GetStatusAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("link-catalog")]
    public async Task<ActionResult<MetaRequirementsStatusDto>> LinkCatalog(
        [FromBody] LinkWhatsAppCatalogRequest request,
        CancellationToken cancellationToken)
        => Ok(await _requirements.LinkCatalogAsync(request.CatalogId, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("connect")]
    public async Task<ActionResult<object>> Connect(
        [FromBody] MetaConnectApiRequest request,
        CancellationToken cancellationToken)
    {
        var credentials = await _metaToken.ConnectAsync(
            new MetaConnectRequest(
                request.UserAccessToken,
                request.FacebookPageId,
                request.InstagramBusinessAccountId,
                request.PageAccessToken,
                request.WhatsAppBusinessAccountId,
                request.WhatsAppCatalogId),
            cancellationToken);

        var checklist = await _requirements.GetStatusAsync(cancellationToken);

        return Ok(new
        {
            message = "Facebook, Instagram and WhatsApp catalog connected.",
            facebookPageId = credentials.FacebookPageId,
            instagramBusinessAccountId = credentials.InstagramBusinessAccountId,
            whatsAppBusinessAccountId = credentials.WhatsAppBusinessAccountId,
            whatsAppCatalogId = credentials.WhatsAppCatalogId,
            requirements = checklist,
        });
    }
}

public record LinkWhatsAppCatalogRequest(string CatalogId);

public record MetaConnectApiRequest(
    string UserAccessToken,
    string? FacebookPageId = null,
    string? InstagramBusinessAccountId = null,
    string? PageAccessToken = null,
    string? WhatsAppBusinessAccountId = null,
    string? WhatsAppCatalogId = null);
