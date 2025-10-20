using Linqyard.Contracts;
using Linqyard.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Linqyard.Contracts.Interfaces;

namespace Linqyard.Api.Controllers;

[Route("profile")]
[Authorize]
public sealed class ProfileController : BaseApiController
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IProfileService _profileService;

    public ProfileController(
        ILogger<ProfileController> logger,
        IProfileService profileService)
    {
        _logger = logger;
        _profileService = profileService;
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

            var profile = await _profileService.GetProfileDetailsAsync(userIdGuid, cancellationToken);
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

            var result = await _profileService.UpdateProfileAsync(userIdGuid, request, cancellationToken);

            switch (result.Status)
            {
                case ProfileUpdateStatus.Success:
                    _logger.LogInformation("Profile updated successfully for user {UserId}", UserId);
                    return OkEnvelope(result.Response!);
                case ProfileUpdateStatus.UserNotFound:
                    _logger.LogWarning("User {UserId} not found", UserId);
                    return NotFoundProblem("User not found");
                case ProfileUpdateStatus.UsernameTaken:
                case ProfileUpdateStatus.InvalidUsername:
                case ProfileUpdateStatus.InvalidDisplayName:
                case ProfileUpdateStatus.InvalidBio:
                    return BadRequestProblem(result.ErrorMessage ?? "Invalid profile data supplied");
                default:
                    return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error",
                        detail: "An error occurred while updating your profile");
            }
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

            var sessionContext = BuildSessionRequest();
            var result = await _profileService.ChangePasswordAsync(
                userIdGuid,
                request,
                sessionContext,
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

            var sessionContext = BuildSessionRequest();
            var sessions = await _profileService.GetSessionsAsync(userIdGuid, sessionContext, cancellationToken);
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

            var result = await _profileService.LogoutFromSessionAsync(userIdGuid, sessionId, cancellationToken);

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

            var sessionContext = BuildSessionRequest();
            var response = await _profileService.LogoutFromAllOtherSessionsAsync(userIdGuid, sessionContext, cancellationToken);

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

            var result = await _profileService.DeleteAccountAsync(userIdGuid, request, cancellationToken);

            switch (result.Status)
            {
                case AccountDeleteServiceStatus.InvalidConfirmation:
                    return BadRequestProblem(result.ErrorMessage ?? "Invalid confirmation text");
                case AccountDeleteServiceStatus.UserNotFound:
                    _logger.LogWarning("User {UserId} not found for deletion", UserId);
                    return NotFoundProblem("User not found");
                case AccountDeleteServiceStatus.InvalidPassword:
                    _logger.LogWarning("Invalid password provided for account deletion by user {UserId}", UserId);
                    return BadRequestProblem(result.ErrorMessage ?? "Password is incorrect");
                case AccountDeleteServiceStatus.Success:
                    break;
                default:
                    _logger.LogError("Unexpected account delete status {Status} for user {UserId}", result.Status, UserId);
                    return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error",
                        detail: "An error occurred while deleting your account");
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

    private ProfileSessionRequest BuildSessionRequest()
    {
        var sessionIdValue = User.FindFirst("sessionId")?.Value ?? User.FindFirst("sid")?.Value;
        Guid? sessionId = Guid.TryParse(sessionIdValue, out var parsedSessionId) ? parsedSessionId : null;

        string? refreshToken = null;
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue) && !string.IsNullOrEmpty(refreshTokenValue))
        {
            refreshToken = refreshTokenValue;
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        return new ProfileSessionRequest(
            sessionId,
            refreshToken,
            ipAddress,
            userAgent);
    }
}
