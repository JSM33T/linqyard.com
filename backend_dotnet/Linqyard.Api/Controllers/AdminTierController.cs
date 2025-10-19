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

[Route("admin/tiers")]
[Authorize(Roles = "admin")]
public sealed class AdminTierController : BaseApiController
{
    private readonly ITierService _tierService;
    private readonly ILogger<AdminTierController> _logger;

    public AdminTierController(
        ITierService tierService,
        ILogger<AdminTierController> logger)
    {
        _tierService = tierService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all tiers with their billing cycles for administrative management.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TierAdminDetailsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTiers(CancellationToken cancellationToken = default)
    {
        var tiers = await _tierService.GetAdminTiersAsync(cancellationToken);
        return OkEnvelope(tiers);
    }

    /// <summary>
    /// Creates a billing cycle for a tier.
    /// </summary>
    [HttpPost("billing-cycles")]
    [ProducesResponseType(typeof(ApiResponse<TierAdminBillingCycleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBillingCycle(
        [FromBody] TierBillingCycleCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var cycle = await _tierService.CreateBillingCycleAsync(request, cancellationToken);
            return OkEnvelope(cycle);
        }
        catch (TierNotFoundException ex)
        {
            _logger.LogWarning(ex, "Attempt to create billing cycle for missing tier {TierId}", request.TierId);
            return NotFoundProblem("Tier not found", ex.Message);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Billing cycle creation failed for tier {TierId}", request.TierId);
            return BadRequestProblem("Unable to create billing cycle", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error creating billing cycle for tier {TierId}", request.TierId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while creating the billing cycle.");
        }
    }

    /// <summary>
    /// Updates an existing billing cycle.
    /// </summary>
    [HttpPut("billing-cycles/{billingCycleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<TierAdminBillingCycleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBillingCycle(
        int billingCycleId,
        [FromBody] TierBillingCycleUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var cycle = await _tierService.UpdateBillingCycleAsync(billingCycleId, request, cancellationToken);
            return OkEnvelope(cycle);
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Billing cycle update failed for cycle {CycleId}", billingCycleId);
            return BadRequestProblem("Unable to update billing cycle", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error updating billing cycle {CycleId}", billingCycleId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while updating the billing cycle.");
        }
    }

    /// <summary>
    /// Deletes a billing cycle.
    /// </summary>
    [HttpDelete("billing-cycles/{billingCycleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBillingCycle(int billingCycleId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tierService.DeleteBillingCycleAsync(billingCycleId, cancellationToken);
            return NoContent();
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Billing cycle delete failed for cycle {CycleId}", billingCycleId);
            return BadRequestProblem("Unable to delete billing cycle", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error deleting billing cycle {CycleId}", billingCycleId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting the billing cycle.");
        }
    }

    /// <summary>
    /// Deletes a billing cycle (enveloped response).
    /// </summary>
    [HttpPost("billing-cycles/{billingCycleId:int}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBillingCyclePost(int billingCycleId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tierService.DeleteBillingCycleAsync(billingCycleId, cancellationToken);
            return OkEnvelope(new { deleted = true });
        }
        catch (TierServiceException ex)
        {
            _logger.LogWarning(ex, "Billing cycle delete failed for cycle {CycleId}", billingCycleId);
            return BadRequestProblem("Unable to delete billing cycle", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error deleting billing cycle {CycleId}", billingCycleId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while deleting the billing cycle.");
        }
    }
}
