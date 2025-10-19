using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Linqyard.Entities;
using Linqyard.Repositories.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linqyard.Repositories;

public sealed class TierService : ITierService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record CouponEvaluation(Coupon Coupon, int DiscountAmount, int FinalAmount);

    private readonly LinqyardDbContext _db;
    private readonly ILogger<TierService> _logger;
    private readonly RazorpaySettings _razorpay;
    private readonly IHttpClientFactory _httpClientFactory;

    public TierService(
        LinqyardDbContext db,
        ILogger<TierService> logger,
        IOptions<RazorpaySettings> razorpayOptions,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _razorpay = razorpayOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var tiers = await _db.Tiers
            .AsNoTracking()
            .Include(t => t.BillingCycles.Where(bc => bc.IsActive))
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var responses = tiers.Select(tier =>
        {
            var plans = new List<TierPlanDetailsResponse>();
            string? description = tier.Description;
            var currency = ResolveCurrency(tier.Currency);

            var orderedCycles = tier.BillingCycles
                .Where(cycle => cycle.IsActive)
                .OrderBy(cycle => cycle.DurationMonths <= 0 ? int.MaxValue : cycle.DurationMonths)
                .ThenBy(cycle => cycle.BillingPeriod, StringComparer.OrdinalIgnoreCase);

            foreach (var cycle in orderedCycles)
            {
                plans.Add(new TierPlanDetailsResponse(
                    cycle.BillingPeriod,
                    cycle.Amount,
                    cycle.DurationMonths,
                    cycle.Description ?? description));
            }

            return new TierDetailsResponse(
                tier.Id,
                tier.Name,
                description,
                currency,
                plans);
        }).ToList();

        return responses;
    }

    public async Task<UserTierInfo?> GetUserActiveTierAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _db.UserTiers
            .AsNoTracking()
            .Where(ut => ut.UserId == userId &&
                         ut.IsActive &&
                         ut.ActiveFrom <= now &&
                         (ut.ActiveUntil == null || ut.ActiveUntil >= now))
            .OrderByDescending(ut => ut.ActiveFrom)
            .Select(ut => new UserTierInfo(
                ut.TierId,
                ut.Tier.Name,
                ut.ActiveFrom,
                ut.ActiveUntil))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TierCouponPreviewResponse> PreviewCouponAsync(
        TierCouponPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.TierName))
        {
            throw new TierServiceException("Tier name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.BillingPeriod))
        {
            throw new TierServiceException("Billing period is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CouponCode))
        {
            throw new TierServiceException("Coupon code is required.");
        }

        var tier = await GetTierByNameAsync(request.TierName, cancellationToken);
        var planContext = await ResolveBillingPlanAsync(tier.Id, request.BillingPeriod, cancellationToken);
        var plan = planContext.BillingCycle;

        var couponEvaluation = await ResolveCouponAsync(
            tier.Id,
            plan.Amount,
            request.CouponCode,
            cancellationToken,
            trackEntity: false,
            requireCoupon: true);

        if (couponEvaluation is null)
        {
            throw new TierServiceException("Unable to apply coupon.");
        }

        var coupon = couponEvaluation.Coupon;
        var currency = ResolveCurrency(tier.Currency);

        return new TierCouponPreviewResponse(
            coupon.Code,
            coupon.DiscountPercentage,
            couponEvaluation.DiscountAmount,
            plan.Amount,
            couponEvaluation.FinalAmount,
            currency,
            coupon.Description,
            coupon.ValidUntil);
    }

    public async Task<TierOrderResponse> CreateUpgradeOrderAsync(
        Guid userId,
        TierUpgradeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.TierName))
        {
            throw new TierServiceException("Tier name is required.");
        }

        var tier = await GetTierByNameAsync(request.TierName, cancellationToken);
        if (tier.Name.Equals("free", StringComparison.OrdinalIgnoreCase))
        {
            throw new TierServiceException("Free tier cannot be purchased.");
        }

        var billingPeriod = (request.BillingPeriod ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(billingPeriod))
        {
            throw new TierServiceException("Billing period is required.");
        }

        var planContext = await ResolveBillingPlanAsync(tier.Id, billingPeriod, cancellationToken);
        var plan = planContext.BillingCycle;
        var couponEvaluation = await ResolveCouponAsync(
            tier.Id,
            plan.Amount,
            request.CouponCode,
            cancellationToken,
            trackEntity: false,
            requireCoupon: false);
        var discountAmount = couponEvaluation?.DiscountAmount ?? 0;
        var finalAmount = couponEvaluation?.FinalAmount ?? plan.Amount;
        var appliedCouponCode = couponEvaluation?.Coupon.Code;

        if (finalAmount <= 0)
        {
            throw new TierServiceException("Coupon reduces the payable amount below the supported minimum.");
        }

        var currentTier = await GetUserActiveTierAsync(userId, cancellationToken);
        if (currentTier is not null && currentTier.TierId == tier.Id)
        {
            throw new TierAlreadyActiveException($"User already has the {tier.Name} tier.");
        }

        EnsureRazorpayConfigured();
        var client = CreateRazorpayClient();
        var currencyCode = ResolveCurrency(tier.Currency);
        var receipt = GenerateReceipt(tier.Name, userId);

        var payload = new
        {
            amount = finalAmount,
            currency = currencyCode,
            receipt,
            payment_capture = 1,
            notes = new
            {
                userId = userId.ToString(),
                tierId = tier.Id,
                tierName = tier.Name,
                billingPeriod = planContext.BillingPeriodKey,
                subtotalAmount = plan.Amount,
                discountAmount,
                couponCode = appliedCouponCode
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create Razorpay order for user {UserId}. Status: {StatusCode}, Body: {Body}",
                userId, response.StatusCode, body);
            throw new TierServiceException("Failed to create payment order. Please try again later.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var orderId = root.GetProperty("id").GetString() ?? throw new TierServiceException("Razorpay order id missing.");
        var amount = root.GetProperty("amount").GetInt32();
        var orderCurrency = root.GetProperty("currency").GetString() ?? currencyCode;
        var returnedReceipt = root.GetProperty("receipt").GetString() ?? receipt;

        _logger.LogInformation("Created Razorpay order {OrderId} for user {UserId} upgrading to {Tier} tier.",
            orderId, userId, tier.Name);

        return new TierOrderResponse(
            tier.Name,
            planContext.BillingPeriodKey,
            orderId,
            returnedReceipt,
            amount,
            orderCurrency,
            _razorpay.KeyId,
            plan.Amount,
            discountAmount,
            appliedCouponCode);
    }

    public async Task<TierUpgradeConfirmationResponse> ConfirmUpgradeAsync(
        Guid userId,
        TierPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.TierName) ||
            string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
            string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
            string.IsNullOrWhiteSpace(request.RazorpaySignature))
        {
            throw new TierServiceException("Invalid payment confirmation payload.");
        }

        var tier = await GetTierByNameAsync(request.TierName, cancellationToken);
        var billingPeriod = (request.BillingPeriod ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(billingPeriod))
        {
            throw new TierServiceException("Billing period is required.");
        }

        var planContext = await ResolveBillingPlanAsync(tier.Id, billingPeriod, cancellationToken);
        var plan = planContext.BillingCycle;
        var couponEvaluation = await ResolveCouponAsync(
            tier.Id,
            plan.Amount,
            request.CouponCode,
            cancellationToken,
            trackEntity: true,
            requireCoupon: !string.IsNullOrWhiteSpace(request.CouponCode));
        var discountAmount = couponEvaluation?.DiscountAmount ?? 0;
        var expectedAmount = couponEvaluation?.FinalAmount ?? plan.Amount;
        var appliedCouponCode = couponEvaluation?.Coupon.Code;

        EnsureRazorpayConfigured();
        VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);

        using var orderDetails = await FetchOrderAsync(request.RazorpayOrderId, cancellationToken);
        ValidateOrder(
            orderDetails.RootElement,
            userId,
            tier,
            expectedAmount,
            planContext.BillingPeriodKey,
            plan.Amount,
            discountAmount,
            appliedCouponCode);

        var now = DateTimeOffset.UtcNow;
        var strategy = _db.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var user = await _db.Users
                .Include(u => u.UserTiers)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                throw new TierServiceException("User not found.");
            }

            foreach (var activeTier in user.UserTiers.Where(ut => ut.IsActive))
            {
                activeTier.IsActive = false;
                activeTier.ActiveUntil = now;
                activeTier.UpdatedAt = now;
            }

            if (couponEvaluation is { Coupon: { } couponEntity })
            {
                if (couponEntity.MaxRedemptions.HasValue &&
                    couponEntity.RedemptionCount >= couponEntity.MaxRedemptions.Value)
                {
                    throw new TierServiceException("Coupon redemption limit reached.");
                }

                couponEntity.RedemptionCount += 1;
                couponEntity.UpdatedAt = now;
            }

            var couponNote = string.IsNullOrWhiteSpace(appliedCouponCode)
                ? string.Empty
                : $" (coupon {appliedCouponCode})";

            var newUserTier = new UserTier
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TierId = tier.Id,
                ActiveFrom = now,
                ActiveUntil = plan.DurationMonths > 0 ? now.AddMonths(plan.DurationMonths) : null,
                IsActive = true,
                Notes = $"Upgraded via Razorpay payment {request.RazorpayPaymentId} ({planContext.BillingPeriodKey}){couponNote}",
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.UserTiers.Add(newUserTier);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var userTierInfo = new UserTierInfo(
                newUserTier.TierId,
                tier.Name,
                newUserTier.ActiveFrom,
                newUserTier.ActiveUntil);

            return new TierUpgradeConfirmationResponse(
                userTierInfo,
                $"Tier upgraded to {tier.Name}",
                planContext.BillingPeriodKey);
        });

        if (!string.IsNullOrWhiteSpace(appliedCouponCode))
        {
            _logger.LogInformation("Confirmed Razorpay payment {PaymentId} for user {UserId}, upgraded to {Tier} with coupon {Coupon}.",
                request.RazorpayPaymentId, userId, tier.Name, appliedCouponCode);
        }
        else
        {
            _logger.LogInformation("Confirmed Razorpay payment {PaymentId} for user {UserId}, upgraded to {Tier}.",
                request.RazorpayPaymentId, userId, tier.Name);
        }

        return result;
    }

    private async Task<Tier> GetTierByNameAsync(string tierName, CancellationToken cancellationToken)
    {
        var normalized = tierName.Trim();
        var tier = await _db.Tiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == normalized, cancellationToken);

        if (tier is null)
        {
            throw new TierNotFoundException($"Unknown tier '{tierName}'.");
        }

        return tier;
    }

    private async Task<(TierBillingCycle BillingCycle, string BillingPeriodKey)> ResolveBillingPlanAsync(
        int tierId,
        string billingPeriod,
        CancellationToken cancellationToken)
    {
        var normalizedPeriod = billingPeriod.Trim();
        if (string.IsNullOrEmpty(normalizedPeriod))
        {
            throw new TierServiceException("Billing period is required.");
        }

        var cycle = await _db.TierBillingCycles
            .AsNoTracking()
            .Where(c => c.TierId == tierId && c.IsActive)
            .FirstOrDefaultAsync(c => c.BillingPeriod == normalizedPeriod, cancellationToken);

        if (cycle is null)
        {
            throw new TierServiceException($"Billing period '{billingPeriod}' is not configured for the selected tier.");
        }

        if (cycle.Amount <= 0)
        {
            throw new TierServiceException($"Billing period '{cycle.BillingPeriod}' must have a positive price configured.");
        }

        if (cycle.DurationMonths < 0)
        {
            throw new TierServiceException($"Billing period '{cycle.BillingPeriod}' has an invalid duration.");
        }

        return (cycle, cycle.BillingPeriod);
    }

    private async Task<CouponEvaluation?> ResolveCouponAsync(
        int tierId,
        int subtotalAmount,
        string? couponCode,
        CancellationToken cancellationToken,
        bool trackEntity,
        bool requireCoupon)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            if (requireCoupon)
            {
                throw new TierServiceException("Coupon code is required.");
            }

            return null;
        }

        var trimmedCode = couponCode.Trim();
        var query = trackEntity ? _db.Coupons : _db.Coupons.AsNoTracking();
        var coupon = await query.FirstOrDefaultAsync(c => c.Code == trimmedCode, cancellationToken);

        if (coupon is null || !coupon.IsActive)
        {
            throw new TierServiceException("Coupon code is invalid or inactive.");
        }

        if (coupon.TierId.HasValue && coupon.TierId.Value != tierId)
        {
            throw new TierServiceException("Coupon is not valid for the selected tier.");
        }

        var now = DateTimeOffset.UtcNow;
        if (coupon.ValidFrom.HasValue && coupon.ValidFrom.Value > now)
        {
            throw new TierServiceException("Coupon is not yet active.");
        }

        if (coupon.ValidUntil.HasValue && coupon.ValidUntil.Value < now)
        {
            throw new TierServiceException("Coupon has expired.");
        }

        if (coupon.MaxRedemptions.HasValue && coupon.RedemptionCount >= coupon.MaxRedemptions.Value)
        {
            throw new TierServiceException("Coupon redemption limit has been reached.");
        }

        if (coupon.DiscountPercentage <= 0)
        {
            throw new TierServiceException("Coupon does not provide a valid discount.");
        }

        var discountAmountDecimal = subtotalAmount * (coupon.DiscountPercentage / 100m);
        var discountAmount = (int)Math.Round(discountAmountDecimal, MidpointRounding.AwayFromZero);

        if (discountAmount <= 0)
        {
            throw new TierServiceException("Coupon does not provide a discount for this billing cycle.");
        }

        if (discountAmount > subtotalAmount)
        {
            discountAmount = subtotalAmount;
        }

        var finalAmount = subtotalAmount - discountAmount;
        if (finalAmount <= 0)
        {
            throw new TierServiceException("Coupon reduces the payable amount below the supported minimum.");
        }

        return new CouponEvaluation(coupon, discountAmount, finalAmount);
    }

    private static string ResolveCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? "INR"
            : currency.Trim().ToUpperInvariant();
    }

    private void EnsureRazorpayConfigured()
    {
        if (string.IsNullOrWhiteSpace(_razorpay.KeyId) ||
            string.IsNullOrWhiteSpace(_razorpay.KeySecret))
        {
            throw new TierServiceException("Razorpay credentials are not configured.");
        }
    }

    private HttpClient CreateRazorpayClient()
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _razorpay.BaseUrl?.Trim();

        client.BaseAddress = string.IsNullOrEmpty(baseUrl)
            ? new Uri("https://api.razorpay.com/v1/")
            : new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");

        var credentials = $"{_razorpay.KeyId}:{_razorpay.KeySecret}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);

        return client;
    }

    private void VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_razorpay.KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        if (!computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentVerificationException("Invalid Razorpay signature.");
        }
    }

    private async Task<JsonDocument> FetchOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var client = CreateRazorpayClient();
        var response = await client.GetAsync($"orders/{orderId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch Razorpay order {OrderId}. Status: {StatusCode}, Body: {Body}",
                orderId, response.StatusCode, body);
            throw new TierServiceException("Unable to validate payment order with Razorpay.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    // Generate a short, unique receipt string no longer than 40 characters.
    // Uses a normalized tier name and a short hex of a SHA-256 over tier|user|timestamp.
    private static string GenerateReceipt(string tierName, Guid userId)
    {
        const int maxTierLen = 12;
        const int hashLen = 16; // 16 hex chars -> 64 bits of uniqueness

        var normalizedTier = string.IsNullOrWhiteSpace(tierName)
            ? "tier"
            : new string(tierName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        if (normalizedTier.Length > maxTierLen)
        {
            normalizedTier = normalizedTier.Substring(0, maxTierLen);
        }

        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var sha = System.Security.Cryptography.SHA256.Create();
        var input = $"{normalizedTier}|{userId:N}|{ts}";
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        var shortHash = hex.Substring(0, hashLen);

        var receipt = $"tier_{normalizedTier}_{shortHash}";

        if (receipt.Length > 40)
        {
            receipt = receipt.Substring(0, 40);
        }

        return receipt;
    }

    private void ValidateOrder(
        JsonElement orderRoot,
        Guid userId,
        Tier tier,
        int expectedAmount,
        string expectedBillingPeriod,
        int expectedSubtotalAmount,
        int expectedDiscountAmount,
        string? expectedCouponCode)
    {
        var status = orderRoot.GetProperty("status").GetString();
        if (!string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentVerificationException("Razorpay order is not marked as paid.");
        }

        var amountPaid = orderRoot.TryGetProperty("amount_paid", out var amountPaidElement)
            ? amountPaidElement.GetInt32()
            : orderRoot.GetProperty("amount").GetInt32();

        if (amountPaid < expectedAmount)
        {
            throw new PaymentVerificationException(
                $"Paid amount {amountPaid} does not match expected amount {expectedAmount}.");
        }

        var orderAmount = orderRoot.GetProperty("amount").GetInt32();
        if (orderAmount != expectedAmount)
        {
            throw new PaymentVerificationException(
                $"Order amount {orderAmount} does not match expected amount {expectedAmount}.");
        }

        if (orderRoot.TryGetProperty("notes", out var notesElement) &&
            notesElement.ValueKind == JsonValueKind.Object)
        {
            if (notesElement.TryGetProperty("userId", out var userIdElement))
            {
                var storedUserId = userIdElement.GetString();
                if (!string.Equals(storedUserId, userId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new PaymentVerificationException("Order was not created for this user.");
                }
            }

            if (notesElement.TryGetProperty("tierId", out var tierIdElement))
            {
                var storedTierId = tierIdElement.GetInt32();
                if (storedTierId != tier.Id)
                {
                    throw new PaymentVerificationException("Order tier mismatch.");
                }
            }

            if (notesElement.TryGetProperty("billingPeriod", out var billingPeriodElement))
            {
                var storedBillingPeriod = billingPeriodElement.GetString();
                if (!string.Equals(storedBillingPeriod, expectedBillingPeriod, StringComparison.OrdinalIgnoreCase))
                {
                    throw new PaymentVerificationException("Order billing period mismatch.");
                }
            }
            else
            {
                throw new PaymentVerificationException("Order is missing billing period metadata.");
            }

            if (notesElement.TryGetProperty("subtotalAmount", out var subtotalElement))
            {
                var storedSubtotal = subtotalElement.GetInt32();
                if (storedSubtotal != expectedSubtotalAmount)
                {
                    throw new PaymentVerificationException("Order subtotal mismatch.");
                }
            }
            else if (expectedDiscountAmount > 0)
            {
                throw new PaymentVerificationException("Order is missing subtotal metadata.");
            }

            var storedDiscount = notesElement.TryGetProperty("discountAmount", out var discountElement)
                ? discountElement.GetInt32()
                : 0;

            if (storedDiscount != expectedDiscountAmount)
            {
                throw new PaymentVerificationException("Order discount mismatch.");
            }

            var storedCouponCode = notesElement.TryGetProperty("couponCode", out var couponElement)
                ? couponElement.GetString()
                : null;

            var normalizedExpectedCoupon = string.IsNullOrWhiteSpace(expectedCouponCode)
                ? null
                : expectedCouponCode.Trim();

            if (!string.Equals(storedCouponCode ?? string.Empty, normalizedExpectedCoupon ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentVerificationException("Order coupon mismatch.");
            }
        }
        else
        {
            throw new PaymentVerificationException("Order metadata is missing.");
        }
    }
}
