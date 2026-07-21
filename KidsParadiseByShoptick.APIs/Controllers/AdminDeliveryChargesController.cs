using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/delivery-charges")]
[Authorize(Roles = "Admin")]
public class AdminDeliveryChargesController : ControllerBase
{
    private readonly IDeliveryChargeService _deliveryCharges;

    public AdminDeliveryChargesController(IDeliveryChargeService deliveryCharges)
        => _deliveryCharges = deliveryCharges;

    [HttpGet]
    public async Task<ActionResult<DeliveryChargeSettingsDto>> Get(CancellationToken cancellationToken)
        => Ok(await _deliveryCharges.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<DeliveryChargeSettingsDto>> Update(
        [FromBody] UpdateDeliveryChargeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _deliveryCharges.UpdateAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
