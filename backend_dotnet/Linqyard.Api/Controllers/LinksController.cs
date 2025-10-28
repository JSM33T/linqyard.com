using Linqyard.Contracts;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Linqyard.Api.Controllers;

[Route($"link")]
[ApiController]
[Authorize]
public sealed class LinksController(ILogger<LinksController> logger, ILinkService linkService) : BaseApiController
{
    /// <summary>
    /// Get links grouped by their group for the authenticated user.
    /// </summary>
    [HttpGet($"")]
    [ProducesResponseType(typeof(ApiResponse<LinksGroupedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLinks(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        try
        {
            var response = await linkService.GetLinksAsync(userId, cancellationToken);
            return OkEnvelope(response);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error getting links for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while retrieving links");
        }
    }

    /// <summary>
    /// Get links grouped by group for a public username (anonymous access).
    /// </summary>
    [HttpGet($"user/{{username}}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LinksGroupedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinksByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await linkService.GetLinksByUsernameAsync(username, cancellationToken);
            return OkEnvelope(response);
        }
        catch (LinkNotFoundException ex)
        {
            logger.LogWarning(ex, "Links not found for username {Username}", username);
            return NotFoundProblem(ex.Message);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error getting links for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while retrieving links");
        }
    }

    /// <summary>
    /// Create a new link.
    /// </summary>
    [HttpPost($"")]
    [ProducesResponseType(typeof(ApiResponse<LinkSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating link for user {UserId} with CorrelationId {CorrelationId}", UserId, CorrelationId);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        try
        {
            var summary = await linkService.CreateLinkAsync(userId, request, cancellationToken);
            return OkEnvelope(summary);
        }
        catch (LinkLimitExceededException ex)
        {
            logger.LogWarning(ex, "Link creation blocked due to tier limits for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (LinkValidationException ex)
        {
            logger.LogWarning(ex, "Validation error while creating link for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (LinkForbiddenException ex)
        {
            logger.LogWarning(ex, "User {UserId} forbidden to create link", userId);
            return ForbiddenProblem(ex.Message);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error creating link for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while creating the link");
        }
    }

    /// <summary>
    /// Edit a link by id (only owner or admin).
    /// </summary>
    [HttpPost($"{{id:guid}}/edit")]
    [ProducesResponseType(typeof(ApiResponse<LinkSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditLink(Guid id, [FromBody] EditLinkRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Editing link {LinkId} by user {UserId}", id, UserId);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        try
        {
            var summary = await linkService.UpdateLinkAsync(id, userId, User.IsInRole("admin"), request, cancellationToken);
            return OkEnvelope(summary);
        }
        catch (LinkNotFoundException ex)
        {
            logger.LogWarning(ex, "Link {LinkId} not found for edit", id);
            return NotFoundProblem(ex.Message);
        }
        catch (LinkForbiddenException ex)
        {
            logger.LogWarning(ex, "User {UserId} forbidden to edit link {LinkId}", userId, id);
            return ForbiddenProblem(ex.Message);
        }
        catch (LinkValidationException ex)
        {
            logger.LogWarning(ex, "Validation error while editing link {LinkId}", id);
            return BadRequestProblem(ex.Message);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error editing link {LinkId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while editing the link");
        }
    }

    /// <summary>
    /// Resequence links (and optionally move them between groups).
    /// Body: [{ "id": "...", "groupId": "...|null", "sequence": 0 }, ...]
    /// </summary>
    [HttpPost("resequence")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Resequence([FromBody] List<ResequenceItem>? items, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RESEQUENCE START: {Count} items", items?.Count ?? 0);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        if (items is null || items.Count == 0)
            return BadRequestProblem("No items provided");

        try
        {
            var finalState = await linkService.ResequenceLinksAsync(userId, items, cancellationToken);
            return OkEnvelope(new
            {
                Message = "Resequenced exactly as specified",
                FinalState = finalState
            });
        }
        catch (LinkForbiddenException ex)
        {
            logger.LogWarning(ex, "User {UserId} forbidden to resequence links", userId);
            return ForbiddenProblem(ex.Message);
        }
        catch (LinkValidationException ex)
        {
            logger.LogWarning(ex, "Validation error while resequencing links for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error resequencing links for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while resequencing links");
        }
    }

    /// <summary>
    /// Delete a link by id (only owner or admin).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLink(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting link {LinkId} by user {UserId}", id, UserId);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        try
        {
            await linkService.DeleteLinkAsync(id, userId, User.IsInRole("admin"), cancellationToken);
            return OkEnvelope(new { Message = "Link deleted" });
        }
        catch (LinkNotFoundException ex)
        {
            logger.LogWarning(ex, "Link {LinkId} not found for delete", id);
            return NotFoundProblem(ex.Message);
        }
        catch (LinkForbiddenException ex)
        {
            logger.LogWarning(ex, "User {UserId} forbidden to delete link {LinkId}", userId, id);
            return ForbiddenProblem(ex.Message);
        }
        catch (LinkServiceException ex)
        {
            logger.LogError(ex, "Error deleting link {LinkId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while deleting the link");
        }
    }
}

