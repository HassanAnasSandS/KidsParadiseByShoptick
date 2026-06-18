using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IFileStorageService _fileStorage;

    public ReviewsController(IReviewService reviewService, IFileStorageService fileStorage)
    {
        _reviewService = reviewService;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _reviewService.GetAllAsync(cancellationToken));

    [HttpGet("toy/{toyId:int}")]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetByToy(int toyId, CancellationToken cancellationToken)
        => Ok(await _reviewService.GetByToyIdAsync(toyId, cancellationToken));

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PendingReviewDto>>> GetPending(
        [FromQuery] string email, CancellationToken cancellationToken)
        => Ok(await _reviewService.GetPendingForCustomerAsync(email, cancellationToken));

    [HttpPost("upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Invalid file type." });

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveImageAsync(stream, file.FileName, "reviews", cancellationToken);
        return Ok(new UploadResponse(path, _fileStorage.GetPublicUrl(path)));
    }

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reviewService.CreateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _reviewService.GetAllAdminAsync(cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewDto>> Update(
        int id, [FromBody] AdminUpdateReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reviewService.UpdateAdminAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
