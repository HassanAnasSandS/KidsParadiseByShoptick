using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDeliveryChargeService _deliveryCharge;

    public OrdersController(IOrderService orderService, IDeliveryChargeService deliveryCharge)
    {
        _orderService = orderService;
        _deliveryCharge = deliveryCharge;
    }

    [HttpPost]
    public async Task<ActionResult<OrderPlacedDto>> PlaceOrder(
        [FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orderService.PlaceOrderAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("track")]
    public async Task<ActionResult<PagedResult<OrderDto>>> Track(
        [FromQuery] string whatsapp,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(whatsapp))
            return BadRequest(new { message = "WhatsApp number is required." });

        var result = await _orderService.GetOrdersByWhatsappPagedAsync(whatsapp, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("delivery-charge")]
    public ActionResult<object> GetDeliveryCharge([FromQuery] string city)
        => Ok(new { deliveryCharge = _deliveryCharge.Calculate(city) });
}
