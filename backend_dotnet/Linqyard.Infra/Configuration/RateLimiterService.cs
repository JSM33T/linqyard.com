using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Linqyard.Contracts.Interfaces;
using Linqyard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Linqyard.Infra.Configuration;

public sealed class RateLimiterService : IRateLimiterService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDbContextFactory<LinqyardDbContext> _dbContextFactory;
    private readonly IOptionsMonitor<RateLimitOptions> _optionsMonitor;
    private readonly ILogger<RateLimiterService> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks =
        new(StringComparer.Ordinal);

    private const int DefaultKeyMaxLength = 256;

    public RateLimiterService(
        IMemoryCache memoryCache,
        IDbContextFactory<LinqyardDbContext> dbContextFactory,
        IOptionsMonitor<RateLimitOptions> optionsMonitor,
        ILogger<RateLimiterService> logger,
        TimeProvider timeProvider)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RateLimitDecision> ShouldAllowAsync(
        string policyName,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name cannot be empty.", nameof(policyName));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Rate-limit key cannot be empty.", nameof(key));
        }

        var normalizedPolicy = policyName.Trim();
        var normalizedKey = NormalizeKey(key);
        var storageKey = BuildStorageKey(normalizedPolicy, normalizedKey);
        var cacheKey = $"rl::{storageKey}";
        var options = _optionsMonitor.CurrentValue;
        var policy = ResolvePolicy(options, normalizedPolicy);
        var timestamp = _timeProvider.GetUtcNow();

        if (!policy.IsActive)
        {
            return RateLimitDecision.Allow(
                normalizedPolicy,
                int.MaxValue,
                0,
                timestamp,
                timestamp,
                timestamp);
        }

        var window = policy.Window;
        var windowStart = AlignToWindow(timestamp, window);
        var windowEnd = windowStart.Add(window);

        var waitDuration = options.LockTimeout;
        var gate = Locks.GetOrAdd(storageKey, static _ => new SemaphoreSlim(1, 1));
        var lockAcquired = false;

        try
        {
            if (waitDuration <= TimeSpan.Zero || waitDuration == Timeout.InfiniteTimeSpan)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockAcquired = true;
            }
            else
            {
                lockAcquired = await gate.WaitAsync(waitDuration, cancellationToken).ConfigureAwait(false);
            }

            if (!lockAcquired)
            {
                _logger.LogWarning(
                    "Rate limiter lock timeout for policy {Policy} and key {Key}; allowing request.",
                    normalizedPolicy,
                    storageKey);

                return RateLimitDecision.Allow(
                    normalizedPolicy,
                    policy.PermitLimit,
                    0,
                    windowStart,
                    windowEnd,
                    timestamp);
            }

            var state = await GetOrLoadStateAsync(cacheKey, storageKey, windowStart, windowEnd, cancellationToken)
                .ConfigureAwait(false);

            if (state.Count >= policy.PermitLimit)
            {
                return BuildRejection(normalizedPolicy, policy.PermitLimit, state.Count, windowStart, windowEnd, timestamp);
            }

            var updatedCount = await TryIncrementBucketAsync(storageKey, windowStart, policy.PermitLimit, cancellationToken)
                .ConfigureAwait(false);

            if (updatedCount is null)
            {
                var refreshedState = state.Count == policy.PermitLimit
                    ? state
                    : state with { Count = policy.PermitLimit };

                CacheState(cacheKey, refreshedState);

                return BuildRejection(normalizedPolicy, policy.PermitLimit, refreshedState.Count, windowStart, windowEnd, timestamp);
            }

            var currentState = new BucketState(windowStart, windowEnd, updatedCount.Value);
            CacheState(cacheKey, currentState);

            return RateLimitDecision.Allow(
                normalizedPolicy,
                policy.PermitLimit,
                currentState.Count,
                windowStart,
                windowEnd,
                timestamp);
        }
        finally
        {
            if (lockAcquired)
            {
                gate.Release();
            }
        }
    }

    private RateLimitPolicy ResolvePolicy(RateLimitOptions options, string policyName)
    {
        if (options.Policies.TryGetValue(policyName, out var configured))
        {
            return configured;
        }

        var fallback = options.Policies
            .FirstOrDefault(pair => string.Equals(pair.Key, policyName, StringComparison.OrdinalIgnoreCase)).Value;
        if (fallback is not null)
        {
            return fallback;
        }

        if (options.DefaultPolicy is not null)
        {
            return options.DefaultPolicy;
        }

        if (options.ThrowOnMissingPolicy)
        {
            throw new InvalidOperationException($"No rate limit policy named '{policyName}' was configured.");
        }

        _logger.LogDebug("No policy named {Policy} configured; treating as unlimited.", policyName);
        return new RateLimitPolicy
        {
            Enabled = false,
            PermitLimit = int.MaxValue,
            Window = TimeSpan.FromHours(1)
        };
    }

    private static string NormalizeKey(string key) =>
        key.Trim().ToLowerInvariant();

    private static string BuildStorageKey(string policyName, string normalizedKey)
    {
        var composite = $"{policyName}:{normalizedKey}";
        if (composite.Length <= DefaultKeyMaxLength)
        {
            return composite;
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(composite));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static DateTimeOffset AlignToWindow(DateTimeOffset timestamp, TimeSpan window)
    {
        var ticks = timestamp.UtcTicks - (timestamp.UtcTicks % window.Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private async Task<BucketState> GetOrLoadStateAsync(
        string cacheKey,
        string storageKey,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(cacheKey, out BucketState? cached) && cached?.WindowStart == windowStart)
        {
            return cached;
        }

        var count = await LoadCountAsync(storageKey, windowStart, cancellationToken).ConfigureAwait(false);
        var state = new BucketState(windowStart, windowEnd, count);
        CacheState(cacheKey, state);
        return state;
    }

    private void CacheState(string cacheKey, BucketState state)
    {
        _memoryCache.Set(
            cacheKey,
            state,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = state.WindowEnd,
                Priority = CacheItemPriority.Normal
            });
    }

    private async Task<int> LoadCountAsync(
        string storageKey,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var count = await context.RateLimitBuckets
            .AsNoTracking()
            .Where(b => b.Key == storageKey && b.WindowStart == windowStart)
            .Select(b => (int?)b.Count)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return count ?? 0;
    }

    private async Task<int?> TryIncrementBucketAsync(
        string storageKey,
        DateTimeOffset windowStart,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var connection = context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            @"INSERT INTO ""RateLimitBuckets"" (""Id"", ""Key"", ""WindowStart"", ""Count"")
              VALUES (@id, @key, @window_start, 1)
              ON CONFLICT (""Key"", ""WindowStart"")
              DO UPDATE SET ""Count"" = ""RateLimitBuckets"".""Count"" + 1
              WHERE ""RateLimitBuckets"".""Count"" < @limit
              RETURNING ""Count"";";

        command.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
        command.Parameters.Add(new NpgsqlParameter("key", storageKey));
        command.Parameters.Add(new NpgsqlParameter("window_start", windowStart));
        command.Parameters.Add(new NpgsqlParameter("limit", limit));

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return result switch
        {
            null => null,
            DBNull => null,
            _ => Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static RateLimitDecision BuildRejection(
        string policyName,
        int limit,
        int currentCount,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        DateTimeOffset timestamp) =>
        RateLimitDecision.Deny(
            policyName,
            limit,
            Math.Max(currentCount, limit),
            windowStart,
            windowEnd,
            timestamp,
            windowEnd,
            "Too many requests.");

    private sealed record BucketState(DateTimeOffset WindowStart, DateTimeOffset WindowEnd, int Count);
}
