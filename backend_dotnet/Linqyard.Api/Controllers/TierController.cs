using Linqyard.Contracts;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Linqyard.Api.Controllers;

[Route("tiers")]
[ApiController]
public sealed class TierController : BaseApiController
{
    private readonly ITierService _tierService;
    private readonly ILogger<TierController> _logger;

    public TierController(ILogger<TierController> logger, ITierService tierService)
    {
        _logger = logger;
        _tierService = tierService;
    }

    /// <summary>
    /// Get all tiers with pricing information.
    /// </summary>
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TierDetailsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTiers(CancellationToken cancellationToken = default)
    {
        var tiers = await _tierService.GetAvailableTiersAsync(cancellationToken);
        return OkEnvelope(tiers);
    }

    /// <summary>
    /// Get the current authenticated user's active tier (if any).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserTierInfo?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTier(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(UserId, out var userIdGuid))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        var tier = await _tierService.GetUserActiveTierAsync(userIdGuid, cancellationToken);
        return OkEnvelope(tier);
    }

    /// <summary>
    /// Validate a coupon against a tier and billing period.
    /// </summary>
    [HttpPost("coupons/preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TierCouponPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewCoupon(
        [FromBody] TierCouponPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.TierName) ||
            string.IsNullOrWhiteSpace(request.BillingPeriod) ||
            string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return BadRequestProblem("Tier name, billing period, and coupon code are required.");
        }

        try
        {
            Guid? userIdGuid = null;
            if (Guid.TryParse(UserId, out var parsedUserId))
            {
                userIdGuid = parsedUserId;
            }

            var preview = await _tierService.PreviewCouponAsync(userIdGuid, request, cancellationToken);
            return OkEnvelope(preview);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tier not found while previewing coupon {CouponCode}", request.CouponCode);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Coupon preview failed for tier {TierName}", request.TierName);
            return BadRequestProblem("Unable to apply coupon", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while previewing coupon for tier {TierName}", request.TierName);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while previewing the coupon.");
        }
    }

    /// <summary>
    /// Create a Razorpay order so the authenticated user can upgrade their tier.
    /// </summary>
    [HttpPost("upgrade/order")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TierOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUpgradeOrder(
        [FromBody] TierUpgradeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(UserId, out var userIdGuid))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.BillingPeriod))
        {
            return BadRequestProblem("Billing period is required.");
        }

        try
        {
            var order = await _tierService.CreateUpgradeOrderAsync(userIdGuid, request, cancellationToken);
            return OkEnvelope(order);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tier not found while creating order for user {UserId}", userIdGuid);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (TierAlreadyActiveException ex)
        {
            _logger.LogWarning(ex, "Tier already active for user {UserId}", userIdGuid);
            return ConflictProblem("Tier already active", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Tier order creation failed for user {UserId}", userIdGuid);
            return BadRequestProblem("Unable to create tier order", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error creating tier order for user {UserId}", userIdGuid);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while creating the order.");
        }
    }

    /// <summary>
    /// Confirm Razorpay payment and activate the purchased tier for the authenticated user.
    /// </summary>
    [HttpPost("upgrade/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TierUpgradeConfirmationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmUpgrade(
        [FromBody] TierPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(UserId, out var userIdGuid))
        {
            return UnauthorizedProblem("Invalid user context");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.BillingPeriod))
        {
            return BadRequestProblem("Billing period is required.");
        }

        try
        {
            var result = await _tierService.ConfirmUpgradeAsync(userIdGuid, request, cancellationToken);
            return OkEnvelope(result);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tier not found while confirming payment for user {UserId}", userIdGuid);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (PaymentVerificationException ex)
        {
            _logger.LogWarning(ex, "Payment verification failed for user {UserId}", userIdGuid);
            return BadRequestProblem("Payment verification failed", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Tier upgrade failed for user {UserId}", userIdGuid);
            return BadRequestProblem("Unable to upgrade tier", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error confirming tier upgrade for user {UserId}", userIdGuid);
            return Problem(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while confirming the upgrade.");
        }
    }
}
