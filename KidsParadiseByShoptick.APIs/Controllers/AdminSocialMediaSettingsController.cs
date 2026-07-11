using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/social-media-settings")]
[Authorize(Roles = "Admin")]
public class AdminSocialMediaSettingsController : ControllerBase
{
    private readonly ISocialMediaSettingsService _settings;

    public AdminSocialMediaSettingsController(ISocialMediaSettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<ActionResult<SocialMediaSettingsDto>> Get(CancellationToken cancellationToken)
        => Ok(await _settings.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<SocialMediaSettingsDto>> Update(
        [FromBody] UpdateSocialMediaSettingsRequest request, CancellationToken cancellationToken)
        => Ok(await _settings.UpdateAsync(request, cancellationToken));
}
