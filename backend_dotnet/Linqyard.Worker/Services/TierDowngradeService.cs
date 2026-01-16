using Linqyard.Data;
using Linqyard.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Linqyard.Worker.Services;

public class TierDowngradeService
{
    private readonly LinqyardDbContext _db;
    private readonly ILogger<TierDowngradeService> _logger;
    private readonly IConfiguration _configuration;

    public TierDowngradeService(
        LinqyardDbContext db,
        ILogger<TierDowngradeService> logger,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ProcessExpiredTiersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _logger.LogInformation("TierDowngrade: checking for expired tiers at {Time}", now);

        // Find all expired tiers (Active tiers where ActiveUntil has passed)
        var expiredTiers = await _db.UserTiers
            .Include(ut => ut.Tier)
            .Include(ut => ut.User)
            .Where(ut => ut.IsActive &&
                         ut.ActiveUntil != null &&
                         ut.ActiveUntil < now)
            .OrderBy(ut => ut.UserId)
            .ThenByDescending(ut => ut.ActiveFrom)
            .ToListAsync(cancellationToken);

        if (expiredTiers.Count == 0)
        {
            _logger.LogInformation("TierDowngrade: no expired tiers found");
            return;
        }

        _logger.LogInformation("TierDowngrade: found {Count} expired tier(s)", expiredTiers.Count);

        // Get the free tier
        var freeTier = await _db.Tiers
            .FirstOrDefaultAsync(t => t.Id == (int)TierType.Free, cancellationToken);

        if (freeTier is null)
        {
            _logger.LogError("TierDowngrade: Free tier not found in database. Cannot downgrade users.");
            return;
        }

        int downgraded = 0;
        int errors = 0;

        foreach (var expiredTier in expiredTiers)
        {
            try
            {
                // Mark the expired tier as inactive
                expiredTier.IsActive = false;
                expiredTier.UpdatedAt = now;

                // Create new free tier for the user
                var newFreeTier = new Entities.UserTier
                {
                    Id = Guid.NewGuid(),
                    UserId = expiredTier.UserId,
                    TierId = freeTier.Id,
                    ActiveFrom = now,
                    ActiveUntil = null, // Free tier doesn't expire
                    IsActive = true,
                    Notes = $"Auto-downgraded from {expiredTier.Tier.Name} due to expiration",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _db.UserTiers.Add(newFreeTier);

                _logger.LogInformation(
                    "TierDowngrade: User {UserId} downgraded from {OldTier} (expired {ExpiredAt}) to Free tier",
                    expiredTier.UserId,
                    expiredTier.Tier.Name,
                    expiredTier.ActiveUntil);

                downgraded++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "TierDowngrade: Failed to downgrade user {UserId} from tier {TierId}",
                    expiredTier.UserId,
                    expiredTier.TierId);
                errors++;
            }
        }

        // Save all changes in one transaction
        if (downgraded > 0)
        {
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "TierDowngrade: Successfully downgraded {Count} user(s) to Free tier. Errors: {Errors}",
                    downgraded,
                    errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TierDowngrade: Failed to save changes to database");
                throw;
            }
        }
    }
}
