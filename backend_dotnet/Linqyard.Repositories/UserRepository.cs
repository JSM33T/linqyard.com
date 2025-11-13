using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Linqyard.Contracts.Requests;
using Linqyard.Entities;

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
        var now = DateTimeOffset.UtcNow;

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
                ActiveTier = u.UserTiers
                    .Where(ut => ut.IsActive &&
                                 ut.ActiveFrom <= now &&
                                 (ut.ActiveUntil == null || ut.ActiveUntil >= now))
                    .OrderByDescending(ut => ut.ActiveFrom)
                    .Select(ut => new UserTierInfo(
                        ut.TierId,
                        ut.Tier.Name,
                        ut.ActiveFrom,
                        ut.ActiveUntil
                    ))
                    .FirstOrDefault()
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
            user.ActiveTier
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

    public async Task<(IReadOnlyList<AdminUserListItemResponse> Users, long Total)> SearchAdminUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = (search ?? string.Empty).Trim();
        var now = DateTimeOffset.UtcNow;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;

        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.Username, pattern) ||
                (u.FirstName != null && EF.Functions.ILike(u.FirstName, pattern)) ||
                (u.LastName != null && EF.Functions.ILike(u.LastName, pattern)) ||
                (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, pattern)));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Username)
            .Skip(skip)
            .Take(pageSize)
            .Select(u => new AdminUserListItemResponse(
                u.Id,
                u.Email,
                u.EmailVerified,
                u.Username,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                u.VerifiedBadge,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt,
                u.UserTiers
                    .Where(ut => ut.IsActive &&
                                 ut.ActiveFrom <= now &&
                                 (ut.ActiveUntil == null || ut.ActiveUntil >= now))
                    .OrderByDescending(ut => ut.ActiveFrom)
                    .Select(ut => new UserTierInfo(
                        ut.TierId,
                        ut.Tier.Name,
                        ut.ActiveFrom,
                        ut.ActiveUntil))
                    .FirstOrDefault(),
                u.UserRoles
                    .OrderBy(ur => ur.Role.Name)
                    .Select(ur => ur.Role.Name)
                    .ToArray()
            ))
            .ToListAsync(cancellationToken);

        return (users, total);
    }

    public async Task<AdminUserDetailsResponse?> GetAdminUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await (
            from u in _db.Users.AsNoTracking()
            where u.Id == userId && u.DeletedAt == null
            let activeTier = u.UserTiers
                .Where(ut => ut.IsActive &&
                             ut.ActiveFrom <= now &&
                             (ut.ActiveUntil == null || ut.ActiveUntil >= now))
                .OrderByDescending(ut => ut.ActiveFrom)
                .Select(ut => new UserTierInfo(
                    ut.TierId,
                    ut.Tier.Name,
                    ut.ActiveFrom,
                    ut.ActiveUntil))
                .FirstOrDefault()
            let roles = u.UserRoles
                .OrderBy(ur => ur.Role.Name)
                .Select(ur => ur.Role.Name)
                .ToArray()
            select new
            {
                Profile = new ProfileDetailsResponse(
                    u.Id,
                    u.Email,
                    u.EmailVerified,
                    u.Username,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Bio,
                    u.AvatarUrl,
                    u.CoverUrl,
                    u.Timezone,
                    u.Locale,
                    u.VerifiedBadge,
                    u.CreatedAt,
                    u.UpdatedAt,
                    roles,
                    activeTier != null ? activeTier.TierId : (int?)null,
                    activeTier != null ? activeTier.Name : null,
                    activeTier
                ),
                ActiveTier = activeTier,
                u.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var tierHistory = await _db.UserTiers
            .AsNoTracking()
            .Where(ut => ut.UserId == userId)
            .OrderByDescending(ut => ut.ActiveFrom)
            .ThenByDescending(ut => ut.CreatedAt)
            .Select(ut => new AdminUserTierAssignmentResponse(
                ut.Id,
                ut.TierId,
                ut.Tier.Name,
                ut.ActiveFrom,
                ut.ActiveUntil,
                ut.IsActive,
                ut.Notes,
                ut.CreatedAt,
                ut.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new AdminUserDetailsResponse(user.Profile, user.ActiveTier, tierHistory, user.IsActive);
    }

    public async Task<AdminUserDetailsResponse?> UpdateAdminUserAsync(
        Guid userId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (AdminUserDetailsResponse?)null;
            }

            static string? Sanitize(string? value) =>
                value is null ? null : (string.IsNullOrWhiteSpace(value) ? null : value.Trim());

            if (request.Email is not null)
            {
                var email = request.Email.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Email cannot be empty");
                }

                var emailInUse = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id != userId && EF.Functions.ILike(u.Email, email), cancellationToken);

                if (emailInUse)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Email is already in use");
                }

                user.Email = email;
            }

            if (request.EmailVerified.HasValue)
            {
                user.EmailVerified = request.EmailVerified.Value;
            }

            if (request.Username is not null)
            {
                var username = request.Username.Trim();
                if (string.IsNullOrEmpty(username))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Username cannot be empty");
                }

                var usernameTaken = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id != userId && EF.Functions.ILike(u.Username, username), cancellationToken);

                if (usernameTaken)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Username is already taken");
                }

                user.Username = username;
            }

            user.FirstName = request.FirstName is null ? user.FirstName : Sanitize(request.FirstName);
            user.LastName = request.LastName is null ? user.LastName : Sanitize(request.LastName);
            user.DisplayName = request.DisplayName is null ? user.DisplayName : Sanitize(request.DisplayName);
            user.Bio = request.Bio is null ? user.Bio : Sanitize(request.Bio);
            user.AvatarUrl = request.AvatarUrl is null ? user.AvatarUrl : Sanitize(request.AvatarUrl);
            user.CoverUrl = request.CoverUrl is null ? user.CoverUrl : Sanitize(request.CoverUrl);
            user.Timezone = request.Timezone is null ? user.Timezone : Sanitize(request.Timezone);
            user.Locale = request.Locale is null ? user.Locale : Sanitize(request.Locale);

            if (request.VerifiedBadge.HasValue)
            {
                user.VerifiedBadge = request.VerifiedBadge.Value;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.Roles is not null)
            {
                var desiredRoles = request.Roles
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var roleEntities = await _db.Roles
                    .Where(r => desiredRoles.Contains(r.Name))
                    .ToListAsync(cancellationToken);

                if (roleEntities.Count != desiredRoles.Length)
                {
                    var missing = desiredRoles
                        .Except(roleEntities.Select(r => r.Name), StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException($"Unknown roles: {string.Join(", ", missing)}");
                }

                var desiredSet = new HashSet<string>(desiredRoles, StringComparer.OrdinalIgnoreCase);

                var rolesToRemove = user.UserRoles
                    .Where(ur => !desiredSet.Contains(ur.Role.Name))
                    .ToList();

                foreach (var remove in rolesToRemove)
                {
                    user.UserRoles.Remove(remove);
                    _db.UserRoles.Remove(remove);
                }

                var currentRoleNames = new HashSet<string>(user.UserRoles.Select(ur => ur.Role.Name), StringComparer.OrdinalIgnoreCase);

                foreach (var role in roleEntities)
                {
                    if (!currentRoleNames.Contains(role.Name))
                    {
                        var link = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        };

                        _db.UserRoles.Add(link);
                        user.UserRoles.Add(link);
                    }
                }
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetAdminUserDetailsAsync(userId, cancellationToken);
        });
    }

    public async Task<AdminUserDetailsResponse?> AssignAdminUserTierAsync(
        Guid userId,
        AdminUpgradeUserTierRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.TierId <= 0)
        {
            throw new InvalidOperationException("TierId must be greater than zero.");
        }

        var tier = await _db.Tiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TierId, cancellationToken);

        if (tier is null)
        {
            throw new TierNotFoundException($"Tier with id {request.TierId} was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var effectiveFrom = (request.ActiveFrom ?? now).ToUniversalTime();
        var effectiveUntil = request.ActiveUntil?.ToUniversalTime();

        if (effectiveUntil.HasValue && effectiveUntil.Value <= effectiveFrom)
        {
            throw new InvalidOperationException("ActiveUntil must be later than ActiveFrom.");
        }

        var trimmedNotes = request.Notes?.Trim();
        if (!string.IsNullOrEmpty(trimmedNotes) && trimmedNotes.Length > 512)
        {
            throw new InvalidOperationException("Notes cannot exceed 512 characters.");
        }

        var notes = string.IsNullOrEmpty(trimmedNotes)
            ? "Manual admin tier upgrade"
            : trimmedNotes;

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var user = await _db.Users
                .Include(u => u.UserTiers)
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

            if (user is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (AdminUserDetailsResponse?)null;
            }

            var mutatedAt = DateTimeOffset.UtcNow;

            foreach (var assignment in user.UserTiers.Where(ut => ut.IsActive))
            {
                assignment.IsActive = false;
                var cutoff = effectiveFrom < assignment.ActiveFrom ? assignment.ActiveFrom : effectiveFrom;
                assignment.ActiveUntil = cutoff;
                assignment.UpdatedAt = mutatedAt;
            }

            var newAssignment = new UserTier
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TierId = tier.Id,
                ActiveFrom = effectiveFrom,
                ActiveUntil = effectiveUntil,
                IsActive = true,
                Notes = notes,
                CreatedAt = mutatedAt,
                UpdatedAt = mutatedAt
            };

            _db.UserTiers.Add(newAssignment);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Admin manually assigned tier {TierName} ({TierId}) to user {UserId}. ActiveFrom={ActiveFrom:u}, ActiveUntil={ActiveUntil:u}",
                tier.Name,
                tier.Id,
                userId,
                effectiveFrom,
                effectiveUntil);

            return await GetAdminUserDetailsAsync(userId, cancellationToken);
        });
    }
}
