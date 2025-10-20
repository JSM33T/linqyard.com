using Linqyard.Contracts;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linqyard.Api.Controllers;

[Route("group")]
[ApiController]
public sealed class GroupsController : BaseApiController
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(ILogger<GroupsController> logger, IGroupService groupService)
    {
        _logger = logger;
        _groupService = groupService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LinkGroupResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        try
        {
            var groups = await _groupService.GetGroupsAsync(userId, cancellationToken);
            return OkEnvelope(groups);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while retrieving groups");
        }
    }

    /// <summary>
    /// Get groups for a public username (anonymous access).
    /// </summary>
    [HttpGet("user/{username}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LinkGroupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupsByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = await _groupService.GetGroupsByUsernameAsync(username, cancellationToken);
            return OkEnvelope(groups);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Groups not found for username {Username}", username);
            return NotFoundProblem(ex.Message);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error getting groups for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while retrieving groups");
        }
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(ApiResponse<LinkGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating link group with CorrelationId {CorrelationId}", CorrelationId);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequestProblem("Name is required");
        }

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        try
        {
            var result = await _groupService.CreateGroupAsync(userId, request, cancellationToken);
            return OkEnvelope(result);
        }
        catch (GroupLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Group creation blocked due to tier limits for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (GroupValidationException ex)
        {
            _logger.LogWarning(ex, "Group creation validation error for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error creating group for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while creating the group");
        }
    }

    [HttpPost("{id:guid}/edit")]
    [ProducesResponseType(typeof(ApiResponse<LinkGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditGroup(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editing group {GroupId} with CorrelationId {CorrelationId}", id, CorrelationId);

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        try
        {
            var group = await _groupService.UpdateGroupAsync(
                id,
                userId,
                User.IsInRole("admin"),
                request,
                cancellationToken);

            return OkEnvelope(group);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group {GroupId} not found for edit", id);
            return NotFoundProblem(ex.Message);
        }
        catch (GroupForbiddenException ex)
        {
            _logger.LogWarning(ex, "User {UserId} forbidden to edit group {GroupId}", userId, id);
            return ForbiddenProblem(ex.Message);
        }
        catch (GroupValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while editing group {GroupId}", id);
            return BadRequestProblem(ex.Message);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error editing group {GroupId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while editing the group");
        }
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting group {GroupId} with CorrelationId {CorrelationId}", id, CorrelationId);

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        try
        {
            await _groupService.DeleteGroupAsync(
                id,
                userId,
                User.IsInRole("admin"),
                cancellationToken);

            return OkEnvelope(new { Message = "Group deleted" });
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning(ex, "Group {GroupId} not found for delete", id);
            return NotFoundProblem(ex.Message);
        }
        catch (GroupForbiddenException ex)
        {
            _logger.LogWarning(ex, "User {UserId} forbidden to delete group {GroupId}", userId, id);
            return ForbiddenProblem(ex.Message);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while deleting the group");
        }
    }

    /// <summary>
    /// Resequence groups for the current user. POST-only.
    /// Body: [{ "id": "...", "sequence": 0 }, ...]
    /// </summary>
    [HttpPost("resequence")]
    [ProducesResponseType(typeof(ApiResponse<GroupResequenceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResequenceGroups([FromBody] List<GroupResequenceItemRequest> items, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GROUP RESEQUENCE START: {Count} items", items?.Count ?? 0);

        if (!Guid.TryParse(UserId, out var userId))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        if (items is null || items.Count == 0)
        {
            return BadRequestProblem("No items provided");
        }

        try
        {
            var result = await _groupService.ResequenceGroupsAsync(userId, items, cancellationToken);
            return OkEnvelope(result);
        }
        catch (GroupValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while resequencing groups for user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (GroupServiceException ex)
        {
            _logger.LogError(ex, "Error resequencing groups for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An error occurred while resequencing groups");
        }
    }
}

