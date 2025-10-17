using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Linqyard.Api.Controllers;

[Route("admin/users")]
[Authorize(Roles = "admin")]
public sealed class AdminUsersController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IUserRepository userRepository,
        ILogger<AdminUsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Lists users with optional search and pagination.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AdminUserListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (users, total) = await _userRepository.SearchAdminUsersAsync(search, page, pageSize, cancellationToken);
        return PagedOk(users, page, pageSize, total);
    }

    /// <summary>
    /// Retrieves full details for a specific user including profile and tier history.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAdminUserDetailsAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Admin attempted to fetch missing user {UserId}", userId);
            return NotFoundProblem("User not found");
        }

        return OkEnvelope(user);
    }

    /// <summary>
    /// Updates core profile details for a user.
    /// </summary>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required");
        }

        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return BadRequestProblem(validationError);
        }

        try
        {
            var updated = await _userRepository.UpdateAdminUserAsync(userId, request, cancellationToken);
            if (updated is null)
            {
                _logger.LogWarning("Admin attempted to update missing user {UserId}", userId);
                return NotFoundProblem("User not found");
            }

            _logger.LogInformation("Admin updated user {UserId}", userId);
            return OkEnvelope(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error when admin updated user {UserId}", userId);
            return BadRequestProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating the user profile");
        }
    }

    private static string? ValidateUpdateRequest(AdminUpdateUserRequest request)
    {
        if (request.Email is not null)
        {
            var email = request.Email.Trim();
            if (email.Length == 0)
            {
                return "Email cannot be empty";
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                return "Invalid email address";
            }
        }

        if (request.Username is not null)
        {
            var username = request.Username.Trim();
            if (username.Length < 4)
            {
                return "Username must be at least 3 characters long";
            }

            if (username.Length > 30)
            {
                return "Username cannot exceed 30 characters";
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_.-]+$"))
            {
                return "Username can only contain letters, numbers, underscores, dots, and hyphens";
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName.Trim().Length > 50)
        {
            return "Display name cannot exceed 50 characters";
        }

        if (!string.IsNullOrWhiteSpace(request.Bio) && request.Bio.Trim().Length > 500)
        {
            return "Bio cannot exceed 500 characters";
        }

        if (request.Roles is not null && request.Roles.Count > 0)
        {
            var invalidRole = request.Roles.FirstOrDefault(r => string.IsNullOrWhiteSpace(r));
            if (invalidRole is not null)
            {
                return "Role names cannot be empty";
            }
        }

        return null;
    }
}
