using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Responses;
using Linqyard.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Linqyard.Api.Controllers;

[Route("user")]
[ApiController]
public class UserController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get minimal public profile information for a username.
    /// </summary>
    [HttpGet("{username}/public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserPublicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = (username ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized)) return NotFoundProblem("User not found");

            var user = await _userRepository.GetPublicByUsernameAsync(normalized, cancellationToken);
            if (user == null) return NotFoundProblem("User not found");

            return OkEnvelope(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public user for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving user");
        }
    }
}
