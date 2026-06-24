using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/toys")]
public class ToysController : ControllerBase
{
    private readonly IToyService _toyService;

    public ToysController(IToyService toyService) => _toyService = toyService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ToyListDto>>> Get(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? onSale,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
        => Ok(await _toyService.GetAvailableAsync(categoryId, search, onSale, sort, page, pageSize, cancellationToken));

    [HttpGet("latest")]
    public async Task<ActionResult<PagedResult<ToyListDto>>> GetLatest(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8,
        CancellationToken cancellationToken = default)
        => Ok(await _toyService.GetAvailableAsync(null, null, null, "newest", page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ToyDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _toyService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
