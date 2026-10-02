using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/site-social-links")]
public class SiteSocialLinksController : ControllerBase
{
    private readonly ISiteSocialLinksService _links;

    public SiteSocialLinksController(ISiteSocialLinksService links) => _links = links;

    [HttpGet]
    public async Task<ActionResult<SiteSocialLinksDto>> Get(CancellationToken cancellationToken)
        => Ok(await _links.GetAsync(cancellationToken));
}
