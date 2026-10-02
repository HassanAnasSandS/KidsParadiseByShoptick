using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

/// <summary>
/// Public read-only affiliate portal — partners look up ledger by opaque code only.
/// </summary>
[ApiController]
[Route("api/affiliates")]
public class AffiliatesController : ControllerBase
{
    private readonly IAffiliateService _affiliateService;

    public AffiliatesController(IAffiliateService affiliateService) => _affiliateService = affiliateService;

    [HttpGet("ledger")]
    public async Task<ActionResult<AffiliateLedgerDto>> GetLedgerByCode(
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Partner code is required." });

        var result = await _affiliateService.GetLedgerByCodeAsync(code.Trim(), cancellationToken);
        if (result is null)
            return NotFound(new { message = "No partner found for this code." });

        return Ok(result);
    }
}
