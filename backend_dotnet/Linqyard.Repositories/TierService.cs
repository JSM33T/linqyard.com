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

    private readonly LinqyardDbContext _db;
    private readonly ILogger<TierService> _logger;
    private readonly RazorpaySettings _razorpay;
    private readonly TierPricingOptions _pricing;
    private readonly IHttpClientFactory _httpClientFactory;

    public TierService(
        LinqyardDbContext db,
        ILogger<TierService> logger,
        IOptions<RazorpaySettings> razorpayOptions,
        IOptions<TierPricingOptions> pricingOptions,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _razorpay = razorpayOptions.Value;
        _pricing = pricingOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var tiers = await _db.Tiers
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var responses = tiers.Select(tier =>
        {
            var plans = new List<TierPlanDetailsResponse>();
            string? description = tier.Description;

            if (_pricing.Plans.TryGetValue(tier.Name, out var configuredPlan) &&
                configuredPlan is not null)
            {
                description = configuredPlan.Description ?? description;

                var orderedCycles = configuredPlan.BillingCycles
                    .OrderBy(kvp => kvp.Value.DurationMonths <= 0 ? int.MaxValue : kvp.Value.DurationMonths)
                    .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var (cycleKey, cycleOptions) in orderedCycles)
                {
                    if (cycleOptions is null) continue;

                    plans.Add(new TierPlanDetailsResponse(
                        cycleKey,
                        cycleOptions.Amount,
                        cycleOptions.DurationMonths,
                        cycleOptions.Description ?? configuredPlan.Description ?? tier.Description));
                }
            }

            return new TierDetailsResponse(
                tier.Id,
                tier.Name,
                description,
                _pricing.Currency,
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

        var planContext = ResolveBillingPlan(tier.Name, billingPeriod);
        var plan = planContext.BillingCycle;
        var currentTier = await GetUserActiveTierAsync(userId, cancellationToken);
        if (currentTier is not null && currentTier.TierId == tier.Id)
        {
            throw new TierAlreadyActiveException($"User already has the {tier.Name} tier.");
        }

    EnsureRazorpayConfigured();
    var client = CreateRazorpayClient();

    var receipt = GenerateReceipt(tier.Name, userId);

        var payload = new
        {
            amount = plan.Amount,
            currency = _pricing.Currency,
            receipt,
            payment_capture = 1,
            notes = new
            {
                userId = userId.ToString(),
                tierId = tier.Id,
                tierName = tier.Name,
                billingPeriod = planContext.BillingPeriodKey
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
        var currency = root.GetProperty("currency").GetString() ?? _pricing.Currency;
        var returnedReceipt = root.GetProperty("receipt").GetString() ?? receipt;

        _logger.LogInformation("Created Razorpay order {OrderId} for user {UserId} upgrading to {Tier} tier.",
            orderId, userId, tier.Name);

        return new TierOrderResponse(
            tier.Name,
            planContext.BillingPeriodKey,
            orderId,
            returnedReceipt,
            amount,
            currency,
            _razorpay.KeyId);
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

        var planContext = ResolveBillingPlan(tier.Name, billingPeriod);
        var plan = planContext.BillingCycle;

        EnsureRazorpayConfigured();
        VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);

        using var orderDetails = await FetchOrderAsync(request.RazorpayOrderId, cancellationToken);
        ValidateOrder(orderDetails.RootElement, userId, tier, plan.Amount, planContext.BillingPeriodKey);

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

            var newUserTier = new UserTier
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TierId = tier.Id,
                ActiveFrom = now,
                ActiveUntil = plan.DurationMonths > 0 ? now.AddMonths(plan.DurationMonths) : null,
                IsActive = true,
                Notes = $"Upgraded via Razorpay payment {request.RazorpayPaymentId} ({planContext.BillingPeriodKey})",
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

        _logger.LogInformation("Confirmed Razorpay payment {PaymentId} for user {UserId}, upgraded to {Tier}.",
            request.RazorpayPaymentId, userId, tier.Name);

        return result;
    }

    private async Task<Tier> GetTierByNameAsync(string tierName, CancellationToken cancellationToken)
    {
        var normalized = tierName.Trim();
        var tier = await _db.Tiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == normalized.ToLower(), cancellationToken);

        if (tier is null)
        {
            throw new TierNotFoundException($"Unknown tier '{tierName}'.");
        }

        return tier;
    }

    private (TierPlanOptions PlanOptions, TierBillingCycleOptions BillingCycle, string BillingPeriodKey) ResolveBillingPlan(
        string tierName,
        string billingPeriod)
    {
        if (!_pricing.Plans.TryGetValue(tierName, out var planOptions) || planOptions is null)
        {
            throw new TierServiceException($"Pricing configuration missing for tier '{tierName}'.");
        }

        if (planOptions.BillingCycles.Count == 0)
        {
            throw new TierServiceException($"Tier '{tierName}' does not have any billing cycles configured.");
        }

        var normalizedPeriod = billingPeriod.Trim();
        if (string.IsNullOrEmpty(normalizedPeriod))
        {
            throw new TierServiceException("Billing period is required.");
        }

        var match = planOptions.BillingCycles.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, normalizedPeriod, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(match.Key) || match.Value is null)
        {
            throw new TierServiceException($"Billing period '{billingPeriod}' is not configured for tier '{tierName}'.");
        }

        if (match.Value.Amount <= 0)
        {
            throw new TierServiceException($"Billing period '{match.Key}' for tier '{tierName}' must have a positive price configured.");
        }

        if (match.Value.DurationMonths < 0)
        {
            throw new TierServiceException($"Billing period '{match.Key}' for tier '{tierName}' has an invalid duration.");
        }

        return (planOptions, match.Value, match.Key);
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

    private void ValidateOrder(JsonElement orderRoot, Guid userId, Tier tier, int expectedAmount, string expectedBillingPeriod)
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
        }
        else
        {
            throw new PaymentVerificationException("Order metadata is missing.");
        }
    }
}
