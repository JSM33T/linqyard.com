using System;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Linqyard.Api.Controllers;

[Route("admin/coupons")]
[Authorize(Roles = "admin")]
public sealed class AdminCouponsController : BaseApiController
{
    private readonly ITierService _tierService;
    private readonly ILogger<AdminCouponsController> _logger;

    public AdminCouponsController(ITierService tierService, ILogger<AdminCouponsController> logger)
    {
        _tierService = tierService;
        _logger = logger;
    }

    /// <summary>
    /// Lists coupons for administrative management.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CouponAdminResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCoupons(CancellationToken cancellationToken = default)
    {
        var coupons = await _tierService.GetAdminCouponsAsync(cancellationToken);
        return OkEnvelope(coupons);
    }

    /// <summary>
    /// Creates a coupon.
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(ApiResponse<CouponAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCoupon(
        [FromBody] CouponCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var coupon = await _tierService.CreateCouponAsync(request, cancellationToken);
            return OkEnvelope(coupon);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempt to create coupon for missing tier {TierId}", request.TierId);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Coupon creation failed for code {Code}", request.Code);
            return BadRequestProblem("Unable to create coupon", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error creating coupon {Code}", request.Code);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating the coupon.");
        }
    }

    /// <summary>
    /// Updates a coupon.
    /// </summary>
    [HttpPut("{couponId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CouponAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCoupon(
        Guid couponId,
        [FromBody] CouponUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var coupon = await _tierService.UpdateCouponAsync(couponId, request, cancellationToken);
            return OkEnvelope(coupon);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempt to associate coupon {CouponId} to missing tier {TierId}", couponId, request.TierId);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Coupon update failed for {CouponId}", couponId);
            return BadRequestProblem("Unable to update coupon", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error updating coupon {CouponId}", couponId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while updating the coupon.");
        }
    }

    /// <summary>
    /// Deletes a coupon.
    /// </summary>
    [HttpDelete("{couponId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCoupon(Guid couponId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tierService.DeleteCouponAsync(couponId, cancellationToken);
            return NoContent();
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Coupon delete failed for {CouponId}", couponId);
            return BadRequestProblem("Unable to delete coupon", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error deleting coupon {CouponId}", couponId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting the coupon.");
        }
    }

    /// <summary>
    /// Deletes a coupon (enveloped response).
    /// </summary>
    [HttpPost("{couponId:guid}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCouponPost(Guid couponId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tierService.DeleteCouponAsync(couponId, cancellationToken);
            return OkEnvelope(new { deleted = true });
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Coupon delete failed for {CouponId}", couponId);
            return BadRequestProblem("Unable to delete coupon", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error deleting coupon {CouponId}", couponId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting the coupon.");
        }
    }
}
