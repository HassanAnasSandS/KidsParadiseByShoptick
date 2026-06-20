using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/site-images")]
public class SiteImagesController : ControllerBase
{
    private readonly ISiteImageService _siteImageService;

    public SiteImagesController(ISiteImageService siteImageService) => _siteImageService = siteImageService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyDictionary<string, string>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _siteImageService.GetPublicUrlsAsync(cancellationToken));
}
