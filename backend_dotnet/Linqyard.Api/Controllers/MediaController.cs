using Linqyard.Api.Extensions;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Linqyard.Api.Controllers;

[Route("media")]
[Authorize]
public sealed class MediaController(
    IAzureBlobStorageService blobStorageService,
    IProfileRepository profileRepository,
    ILogger<MediaController> logger)
    : BaseApiController
{
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
            var response = new MediaUploadResponse(uploadResult.BlobName, uploadResult.Url);
            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image upload failed");
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
    [RateLimit("profile-avatar-update", Partition = RateLimitPartitionStrategy.UserId)]
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
            var updateResponse = await profileRepository.UpdateProfileAsync(userId, updateRequest, cancellationToken);

            if (updateResponse is null)
            {
                return NotFoundProblem("User not found", "Unable to update the avatar for the current user.");
            }

            return OkEnvelope(updateResponse, new { uploadResult.BlobName });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Avatar upload failed for user {UserId}", UserId);
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
            var updateResponse = await profileRepository.UpdateProfileAsync(userId, updateRequest, cancellationToken);

            if (updateResponse is null)
            {
                return NotFoundProblem("User not found", "Unable to update the cover image for the current user.");
            }

            return OkEnvelope(updateResponse, new { uploadResult.BlobName });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cover upload failed for user {UserId}", UserId);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Cover upload failed",
                "An unexpected error occurred while uploading the cover image.");
        }
    }



    /// <summary>
    /// Uploads a file to Azure Blob Storage and caches it locally.
    /// </summary>
    private Task<BlobUploadResult> UploadFileAsync(IFormFile file, CancellationToken cancellationToken)
        => UploadFileAsync(file, null, cancellationToken);

    private async Task<BlobUploadResult> UploadFileAsync(IFormFile file, string? blobName, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await blobStorageService.UploadImageAsync(
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            blobName,
            cancellationToken);
    }

    /// <summary>
    /// Retrieves the avatar image for a profile, using the local Azure Blob cache.
    /// </summary>
    [HttpGet("profile/{userId:guid}/avatar")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileAvatar(Guid userId, CancellationToken cancellationToken = default)
        => await GetProfileMediaAsync(
            userId,
            profile => profile.AvatarUrl,
            "Avatar",
            "avatar",
            cancellationToken);

    /// <summary>
    /// Retrieves the cover image for a profile, using the local Azure Blob cache.
    /// </summary>
    [HttpGet("profile/{userId:guid}/cover")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileCover(Guid userId, CancellationToken cancellationToken = default)
        => await GetProfileMediaAsync(
            userId,
            profile => profile.CoverUrl,
            "Cover",
            "cover image",
            cancellationToken);

    /// <summary>
    /// Retrieves an image by its blob name. Uses the local cache when possible.
    /// </summary>
    [HttpGet("{blobName}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return BadRequestProblem("Invalid image identifier", "The blob name provided is empty.");
        }

        var cachedImage = await blobStorageService.GetImageAsync(blobName, cancellationToken);
        if (cachedImage is null)
        {
            return NotFoundProblem("Image not found", $"No image found for blob name '{blobName}'.");
        }

        return StreamCachedImage(
            resourceType: "blob image",
            cacheIdentifier: blobName,
            cachedImage,
            notFoundTitle: "Image not found",
            notFoundDetail: $"No cached file available for blob name '{blobName}'.",
            errorTitle: "Failed to read image");
    }

    private async Task<IActionResult> GetProfileMediaAsync(
        Guid userId,
        Func<ProfileDetailsResponse, string?> mediaSelector,
        string mediaTitle,
        string mediaDescription,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetProfileDetailsAsync(userId, cancellationToken);
        if (profile is null)
        {
            return NotFoundProblem("User not found", $"No profile found for user '{userId}'.");
        }

        var mediaReference = mediaSelector(profile);
        if (string.IsNullOrWhiteSpace(mediaReference))
        {
            return NotFoundProblem($"{mediaTitle} not found", $"The user '{userId}' has not set a {mediaDescription}.");
        }

        var cachedImage = await ResolveCachedImageAsync(mediaReference, cancellationToken);
        
        if (cachedImage is null)
            return NotFoundProblem(
                $"{mediaTitle} not found",
                $"No cached {mediaDescription} available for user '{userId}'.");

        return StreamCachedImage(
            resourceType: $"{mediaDescription}",
            cacheIdentifier: mediaReference,
            cachedImage,
            notFoundTitle: $"{mediaTitle} not found",
            notFoundDetail: $"No cached {mediaDescription} available for user '{userId}'.",
            errorTitle: $"Failed to read {mediaDescription}");
    }

    private async Task<CachedImageResult?> ResolveCachedImageAsync(string mediaReference, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(mediaReference, UriKind.Absolute, out _))
        {
            var cachedFromUrl = await blobStorageService.GetImageByUrlAsync(mediaReference, cancellationToken);
            if (cachedFromUrl is not null)
            {
                return cachedFromUrl;
            }
        }

        return await blobStorageService.GetImageAsync(mediaReference, cancellationToken);
    }

    private IActionResult StreamCachedImage(
        string resourceType,
        string cacheIdentifier,
        CachedImageResult cachedImage,
        string notFoundTitle,
        string notFoundDetail,
        string errorTitle)
    {
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
            logger.LogWarning("Cached file missing for {ResourceType} identifier {CacheIdentifier}", resourceType, cacheIdentifier);
            return NotFoundProblem(notFoundTitle, notFoundDetail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read cached {ResourceType} for identifier {CacheIdentifier}", resourceType, cacheIdentifier);
            return Problem(
                StatusCodes.Status500InternalServerError,
                errorTitle,
                "An unexpected error occurred while reading the cached image.");
        }
    }
}
