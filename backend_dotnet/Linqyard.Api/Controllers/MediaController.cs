using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linqyard.Api.Controllers;

[Route("media")]
[Authorize]
public sealed class MediaController : BaseApiController
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        ICloudinaryService cloudinaryService,
        IProfileRepository profileRepository,
        ILogger<MediaController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    /// <summary>
    /// Uploads an image to Cloudinary and stores a cached copy locally.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MediaUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequestProblem("No file uploaded", "Provide a non-empty file in the request body.");
        }

        try
        {
            var uploadResult = await UploadFileAsync(file, cancellationToken);
            var response = new MediaUploadResponse(uploadResult.PublicId, uploadResult.Url);
            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed");
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Image upload failed",
                "An unexpected error occurred while uploading the image.");
        }
    }

    /// <summary>
    /// Uploads an avatar image, updates the current user's profile, and returns the updated profile.
    /// </summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(ApiResponse<ProfileUpdateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequestProblem("No file uploaded", "Provide a non-empty file in the request body.");
        }

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Unauthorized", "Unable to resolve the current user.");
        }

        try
        {
            var uploadResult = await UploadFileAsync(file, $"{userId:N}-avatar", cancellationToken);

            var updateRequest = new UpdateProfileRequest(AvatarUrl: uploadResult.Url);
            var updateResponse = await _profileRepository.UpdateProfileAsync(userId, updateRequest, cancellationToken);

            if (updateResponse is null)
            {
                return NotFoundProblem("User not found", "Unable to update the avatar for the current user.");
            }

            return OkEnvelope(updateResponse, new { uploadResult.PublicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Avatar upload failed for user {UserId}", UserId);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Avatar upload failed",
                "An unexpected error occurred while uploading the avatar.");
        }
    }
    /// <summary>
    /// Uploads a cover image, updates the current user's profile, and returns the updated profile.
    /// </summary>
    [HttpPost("cover")]
    [ProducesResponseType(typeof(ApiResponse<ProfileUpdateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadCover([FromForm] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequestProblem("No file uploaded", "Provide a non-empty file in the request body.");
        }

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Unauthorized", "Unable to resolve the current user.");
        }

        try
        {
            var uploadResult = await UploadFileAsync(file, $"{userId:N}-cover", cancellationToken);

            var updateRequest = new UpdateProfileRequest(CoverUrl: uploadResult.Url);
            var updateResponse = await _profileRepository.UpdateProfileAsync(userId, updateRequest, cancellationToken);

            if (updateResponse is null)
            {
                return NotFoundProblem("User not found", "Unable to update the cover image for the current user.");
            }

            return OkEnvelope(updateResponse, new { uploadResult.PublicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cover upload failed for user {UserId}", UserId);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Cover upload failed",
                "An unexpected error occurred while uploading the cover image.");
        }
    }



    /// <summary>
    /// Uploads a file to Cloudinary and caches it locally.
    /// </summary>
    private Task<CloudinaryUploadResult> UploadFileAsync(IFormFile file, CancellationToken cancellationToken)
        => UploadFileAsync(file, null, cancellationToken);

    private async Task<CloudinaryUploadResult> UploadFileAsync(IFormFile file, string? publicId, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await _cloudinaryService.UploadImageAsync(
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            publicId,
            cancellationToken);
    }

    /// <summary>
    /// Retrieves an image by its Cloudinary public ID. Uses the local cache when possible.
    /// </summary>
    [HttpGet("{publicId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return BadRequestProblem("Invalid image identifier", "The public ID provided is empty.");
        }

        var cachedImage = await _cloudinaryService.GetImageAsync(publicId, cancellationToken);
        if (cachedImage is null)
        {
            return NotFoundProblem("Image not found", $"No image found for public ID '{publicId}'.");
        }

        try
        {
            var stream = new FileStream(
                cachedImage.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return File(stream, cachedImage.ContentType, cachedImage.FileName);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Cached file missing for public ID {PublicId} despite cache entry", publicId);
            return NotFoundProblem("Image not found", $"No cached file available for public ID '{publicId}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read cached image for public ID {PublicId}", publicId);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Failed to read image",
                "An unexpected error occurred while reading the cached image.");
        }
    }
}

