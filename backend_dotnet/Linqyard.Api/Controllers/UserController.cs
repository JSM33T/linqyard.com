using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Responses;
using Linqyard.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Linqyard.Api.Controllers;

[Route($"user")]
[ApiController]
public class UserController(ILogger<UserController> logger, IUserRepository userRepository)
    : BaseApiController
{
    /// <summary>
    /// Get minimal public profile information for a username.
    /// </summary>
    [HttpGet("{username}/public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserPublicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicByUsername(string? username, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = (username ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized)) return NotFoundProblem("User not found");

            var user = await userRepository.GetPublicByUsernameAsync(normalized, cancellationToken);
            return user == null ? NotFoundProblem("User not found") : OkEnvelope(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting public user for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving user");
        }
    }
}
