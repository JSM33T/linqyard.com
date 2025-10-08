using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts;
using Linqyard.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Linqyard.Api.Controllers;

[Route("profile")]
[Authorize]
public sealed class ProfileController : BaseApiController
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IProfileRepository _profileRepository;

    public ProfileController(
        ILogger<ProfileController> logger,
        IProfileRepository profileRepository)
    {
        _logger = logger;
        _profileRepository = profileRepository;
    }

    /// <summary>
    /// Get current user's profile details
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileDetails(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            var profile = await _profileRepository.GetProfileDetailsAsync(userIdGuid, cancellationToken);
            if (profile == null)
            {
                _logger.LogWarning("User {UserId} not found", UserId);
                return NotFoundProblem("User not found");
            }

            return OkEnvelope(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile details for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving your profile");
        }
    }

    /// <summary>
    /// Update user's profile information (excluding password)
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(ApiResponse<ProfileUpdateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating profile for user {UserId} with CorrelationId {CorrelationId}",
            UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            // Username validation
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                if (request.Username.Length < 4)
                {
                    return BadRequestProblem("Username must be at least 3 characters long");
                }

                if (request.Username.Length > 30)
                {
                    return BadRequestProblem("Username cannot exceed 30 characters");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_.-]+$"))
                {
                    return BadRequestProblem("Username can only contain letters, numbers, underscores, dots, and hyphens");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName.Length > 50)
            {
                return BadRequestProblem("Display name cannot exceed 50 characters");
            }

            if (!string.IsNullOrWhiteSpace(request.Bio) && request.Bio.Length > 500)
            {
                return BadRequestProblem("Bio cannot exceed 500 characters");
            }

            var response = await _profileRepository.UpdateProfileAsync(userIdGuid, request, cancellationToken);
            if (response == null)
            {
                _logger.LogWarning("User {UserId} not found", UserId);
                return NotFoundProblem("User not found");
            }

            _logger.LogInformation("Profile updated successfully for user {UserId}", UserId);
            return OkEnvelope(response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Username is already taken")
        {
            _logger.LogWarning("Username already taken for user {UserId}: {Username}", UserId, request.Username);
            return BadRequestProblem("Username is already taken");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating your profile");
        }
    }

    /// <summary>
    /// Change user's password
    /// </summary>
    [HttpPost("password")]
    [ProducesResponseType(typeof(ApiResponse<PasswordChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password change attempt for user {UserId} with CorrelationId {CorrelationId}",
            UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            var currentSession = await GetCurrentSessionAsync(userIdGuid, cancellationToken);
            var signedInViaGoogle = currentSession != null &&
                string.Equals(currentSession.AuthMethod, "google", StringComparison.OrdinalIgnoreCase);

            var result = await _profileRepository.ChangePasswordAsync(
                userIdGuid,
                request.CurrentPassword,
                request.NewPassword,
                signedInViaGoogle,
                cancellationToken);

            return result.Status switch
            {
                PasswordChangeStatus.UserNotFound => NotFoundProblem("User not found"),
                PasswordChangeStatus.InvalidCurrentPassword => BadRequestProblem("Current password is incorrect"),
                PasswordChangeStatus.PasswordTooShort => BadRequestProblem($"New password must be at least {result.MinimumLength} characters long"),
                PasswordChangeStatus.PasswordSame => BadRequestProblem("New password must be different from current password"),
                PasswordChangeStatus.Success => OkEnvelope(result.Response!),
                _ => Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error",
                    detail: "An unknown error occurred while changing your password")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while changing your password");
        }
    }

    /// <summary>
    /// Get all active sessions for the current user
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<SessionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting sessions for user {UserId} with CorrelationId {CorrelationId}",
            UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            var currentSession = await GetCurrentSessionAsync(userIdGuid, cancellationToken);
            var sessions = await _profileRepository.GetSessionsAsync(userIdGuid, currentSession?.Id, cancellationToken);
            return OkEnvelope(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving your sessions");
        }
    }

    /// <summary>
    /// Logout from a specific session (revoke session and its refresh tokens)
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/logout")]
    [ProducesResponseType(typeof(ApiResponse<SessionDeleteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogoutFromSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout from session {SessionId} for user {UserId} with CorrelationId {CorrelationId}",
            sessionId, UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            var result = await _profileRepository.LogoutFromSessionAsync(userIdGuid, sessionId, cancellationToken);

            return result.Status switch
            {
                SessionLogoutStatus.NotFound => NotFoundProblem("Session not found or does not belong to you"),
                SessionLogoutStatus.AlreadyLoggedOut => BadRequestProblem("Session is already logged out"),
                SessionLogoutStatus.Success => OkEnvelope(result.Response!),
                _ => Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error",
                    detail: "An unknown error occurred while logging out the session")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out session {SessionId} for user {UserId}", sessionId, UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while logging out the session");
        }
    }

    /// <summary>
    /// Logout from all other sessions (except current one)
    /// </summary>
    [HttpPost("sessions/logout-all")]
    [ProducesResponseType(typeof(ApiResponse<SessionDeleteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutFromAllOtherSessions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout from all other sessions for user {UserId} with CorrelationId {CorrelationId}",
            UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            var currentSession = await GetCurrentSessionAsync(userIdGuid, cancellationToken);
            var response = await _profileRepository.LogoutFromAllOtherSessionsAsync(userIdGuid, currentSession?.Id, cancellationToken);

            _logger.LogInformation("Logged out from other sessions for user {UserId}", UserId);
            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out from all other sessions for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while logging out from other sessions");
        }
    }

    /// <summary>
    /// Delete user account permanently (requires password confirmation)
    /// </summary>
    [HttpPost("delete")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<AccountDeleteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Account deletion attempt for user {UserId} with CorrelationId {CorrelationId}",
            UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userIdGuid))
            {
                return UnauthorizedProblem("Invalid user context");
            }

            if (request.ConfirmationText != "DELETE MY ACCOUNT")
            {
                return BadRequestProblem("Confirmation text must be exactly: DELETE MY ACCOUNT");
            }

            var result = await _profileRepository.DeleteAccountAsync(userIdGuid, request.Password, cancellationToken);

            if (result.Status == AccountDeleteStatus.UserNotFound)
            {
                _logger.LogWarning("User {UserId} not found for deletion", UserId);
                return NotFoundProblem("User not found");
            }

            if (result.Status == AccountDeleteStatus.InvalidPassword)
            {
                _logger.LogWarning("Invalid password provided for account deletion by user {UserId}", UserId);
                return BadRequestProblem("Password is incorrect");
            }

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/auth"
            });

            _logger.LogInformation("Account deleted successfully for user {UserId}", UserId);
            return OkEnvelope(result.Response!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user {UserId}", UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while deleting your account");
        }
    }

    private async Task<SessionContext?> GetCurrentSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessionIdValue = User.FindFirst("sessionId")?.Value ?? User.FindFirst("sid")?.Value;
        Guid? sessionId = Guid.TryParse(sessionIdValue, out var parsedSessionId) ? parsedSessionId : null;

        string? refreshTokenHash = null;
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue) && !string.IsNullOrEmpty(refreshTokenValue))
        {
            refreshTokenHash = HashToken(refreshTokenValue);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        return await _profileRepository.ResolveCurrentSessionAsync(
            userId,
            sessionId,
            refreshTokenHash,
            ipAddress,
            userAgent,
            cancellationToken);
    }
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
