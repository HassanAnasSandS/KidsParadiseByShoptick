using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/site-social-links")]
[Authorize(Roles = "Admin")]
public class AdminSiteSocialLinksController : ControllerBase
{
    private readonly ISiteSocialLinksService _links;

    public AdminSiteSocialLinksController(ISiteSocialLinksService links) => _links = links;

    [HttpGet]
    public async Task<ActionResult<SiteSocialLinksDto>> Get(CancellationToken cancellationToken)
        => Ok(await _links.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<SiteSocialLinksDto>> Update(
        [FromBody] UpdateSiteSocialLinksRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _links.UpdateAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
