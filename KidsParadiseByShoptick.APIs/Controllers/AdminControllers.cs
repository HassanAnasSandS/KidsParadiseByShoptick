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
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _categoryService.GetAllAdminAsync(cancellationToken));

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
    public async Task<ActionResult<IReadOnlyList<ToyListDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _toyService.GetAllAdminAsync(cancellationToken));

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
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _orderService.GetAllAdminAsync(cancellationToken));

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

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Invalid file type." });

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveImageAsync(stream, file.FileName, folder, cancellationToken);
        return Ok(new UploadResponse(path, _fileStorage.GetPublicUrl(path)));
    }
}
