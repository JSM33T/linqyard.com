using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Linqyard.Repositories;

public sealed class UserRepository(LinqyardDbContext db, ILogger<UserRepository> logger) : IUserRepository
{
    private readonly LinqyardDbContext _db = db;
    private readonly ILogger<UserRepository> _logger = logger;

    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user count from database (EF)");

        // Only non-deleted users
        var count = await _db.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null)
            .CountAsync(cancellationToken);

        _logger.LogInformation("Retrieved user count from database: {UserCount}", count);
        return count;
    }

    public async Task<UserPublicResponse?> GetPublicByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = (username ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        // Case-insensitive username match + not deleted
        // If you use CITEXT in PG, you can compare directly without ToLower.
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null &&
                        u.Username.ToLower() == normalized.ToLower())
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FirstName,
                u.LastName,
                u.AvatarUrl,
                u.CoverUrl,
                u.Bio,
                u.TierId,
                TierName = u.Tier != null ? u.Tier.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return null;

        return new UserPublicResponse(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.CoverUrl,
            user.Bio,
            user.TierId,
            user.TierName
        );
    }

    public async Task<UserBasicResponse?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = (username ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        _logger.LogDebug("Getting user by username: {Username}", normalized);

        // Case-insensitive username match + not deleted
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null &&
                        u.Username.ToLower() == normalized.ToLower())
            .Select(u => new UserBasicResponse(u.Id, u.Username))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}
