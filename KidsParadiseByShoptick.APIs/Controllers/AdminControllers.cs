using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _authService;

    public AdminAuthController(IAdminAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result is null ? Unauthorized(new { message = "Invalid credentials." }) : Ok(result);
    }
}

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public AdminDashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> GetStats(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
        => Ok(await _dashboardService.GetAdminStatsAsync(dateFrom, dateTo, cancellationToken));
}

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? search = null,
        [FromQuery] string? toyFilter = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
        => Ok(await _categoryService.GetAdminPagedAsync(search, toyFilter, sort, page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAdminAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _categoryService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(
        int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _categoryService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/admin/toys")]
[Authorize(Roles = "Admin")]
public class AdminToysController : ControllerBase
{
    private readonly IToyService _toyService;

    public AdminToysController(IToyService toyService) => _toyService = toyService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ToyListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isSold = null,
        [FromQuery] bool? onSale = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
        => Ok(await _toyService.GetAdminPagedAsync(categoryId, search, isSold, onSale, sort, page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ToyDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _toyService.GetByIdAdminAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ToyListDto>> Create(
        [FromBody] CreateToyRequest request, CancellationToken cancellationToken)
        => Ok(await _toyService.CreateAsync(request, cancellationToken));

    [HttpPost("{id:int}/clone")]
    public async Task<ActionResult<ToyListDto>> Clone(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _toyService.CloneAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ToyListDto>> Update(
        int id, [FromBody] UpdateToyRequest request, CancellationToken cancellationToken)
    {
        var result = await _toyService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _toyService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? city = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? sort = "newest",
        CancellationToken cancellationToken = default)
        => Ok(await _orderService.GetAdminPagedAsync(status, search, city, dateFrom, dateTo, sort, page, pageSize, cancellationToken));

    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCities(CancellationToken cancellationToken)
        => Ok(await _orderService.GetAdminCitiesAsync(cancellationToken));

    [HttpGet("status-counts")]
    public async Task<ActionResult<OrderStatusCountsDto>> GetStatusCounts(CancellationToken cancellationToken)
        => Ok(await _orderService.GetAdminStatusCountsAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<OrderPlacedDto>> Create(
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdAdminAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDto>> Update(
        int id, [FromBody] AdminUpdateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orderService.UpdateAdminAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orderService.UpdateStatusAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/admin/upload")]
[Authorize(Roles = "Admin")]
public class AdminUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;

    public AdminUploadController(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadResponse>> Upload(
        IFormFile file, [FromQuery] string folder = "general", CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".jfif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Invalid file type." });

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveImageAsync(stream, file.FileName, folder, cancellationToken);
        return Ok(new UploadResponse(path, _fileStorage.GetPublicUrl(path)));
    }
}

[ApiController]
[Route("api/admin/site-images")]
[Authorize(Roles = "Admin")]
public class AdminSiteImagesController : ControllerBase
{
    private readonly ISiteImageService _siteImageService;

    public AdminSiteImagesController(ISiteImageService siteImageService) => _siteImageService = siteImageService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<SiteImageAdminDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _siteImageService.GetAdminPagedAsync(page, pageSize, cancellationToken));

    [HttpPost("{key}/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<SiteImageAdminDto>> Upload(
        string key, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".jfif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Invalid file type." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _siteImageService.UploadAsync(key, stream, file.FileName, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{key}/custom")]
    public async Task<ActionResult<SiteImageAdminDto>> Reset(string key, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _siteImageService.ResetAsync(key, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
