using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace Linkyard.Repositories;

public class ProfileRepository(LinqyardDbContext db, ILogger<ProfileRepository> logger, IConfiguration configuration) : IProfileRepository
{
    private const string DefaultConnectionName = "DefaultConnection";
    private readonly LinqyardDbContext _db = db;
    private readonly ILogger<ProfileRepository> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task<ProfileDetailsResponse?> GetProfileDetailsAsync(
      Guid userId,
      CancellationToken cancellationToken = default)
    {
        var dto = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
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
                Roles = u.UserRoles
                    .Select(ur => ur.Role.Name)
                    .ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null) return null;

        return new ProfileDetailsResponse(
            dto.Id,
            dto.Email,
            dto.EmailVerified,
            dto.Username,
            dto.FirstName,
            dto.LastName,
            dto.DisplayName,
            dto.Bio,
            dto.AvatarUrl,
            dto.CoverUrl,
            dto.Timezone,
            dto.Locale,
            dto.VerifiedBadge,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.Roles
        );
    }


    public async Task<ProfileUpdateResponse?> UpdateProfileAsync(
    Guid userId,
    UpdateProfileRequest request,
    CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Start a transaction (keeps semantics close to your SELECT ... FOR UPDATE pattern)
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Load the user as a tracked entity for updates
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return null;
        }

        // Keep stable snapshot of fields needed for the response
        var current = new
        {
            user.Id,
            user.Email,
            user.EmailVerified,
            user.Username,
            user.VerifiedBadge,
            user.CreatedAt
        };

        // Helper: trim + convert empty/whitespace to null
        static string? Sanitize(string? value) =>
            value is null ? null : (string.IsNullOrWhiteSpace(value) ? null : value.Trim());

        // Compute “next” values (null in request => keep existing, whitespace => null)
        var username = request.Username is null ? user.Username : Sanitize(request.Username);
        var firstName = request.FirstName is null ? user.FirstName : Sanitize(request.FirstName);
        var lastName = request.LastName is null ? user.LastName : Sanitize(request.LastName);
        var displayName = request.DisplayName is null ? user.DisplayName : Sanitize(request.DisplayName);
        var bio = request.Bio is null ? user.Bio : Sanitize(request.Bio);
        var avatarUrl = request.AvatarUrl is null ? user.AvatarUrl : Sanitize(request.AvatarUrl);
        var coverUrl = request.CoverUrl is null ? user.CoverUrl : Sanitize(request.CoverUrl);
        var timezone = request.Timezone is null ? user.Timezone : Sanitize(request.Timezone);
        var locale = request.Locale is null ? user.Locale : Sanitize(request.Locale);

        // Case-insensitive uniqueness check only if username is changing and not empty
        if (!string.Equals(username, user.Username, StringComparison.Ordinal) &&
            !string.IsNullOrEmpty(username))
        {
            var exists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id != userId &&
                    EF.Functions.ILike(u.Username, username!), // uses ILIKE for PostgreSQL case-insensitive match
                cancellationToken);

            if (exists)
            {
                await tx.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Username is already taken");
            }
        }

        // Apply updates
        user.Username = username ?? user.Username; // keep original if null
        user.FirstName = firstName;
        user.LastName = lastName;
        user.DisplayName = displayName;
        user.Bio = bio;
        user.AvatarUrl = avatarUrl;
        user.CoverUrl = coverUrl;
        user.Timezone = timezone;
        user.Locale = locale;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Fetch roles for response (no Include needed if you prefer a separate query)
        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles,
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => r.Name)
            .ToArrayAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var profile = new ProfileDetailsResponse(
            current.Id,
            current.Email,
            current.EmailVerified,
            user.Username,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.Bio,
            user.AvatarUrl,
            user.CoverUrl,
            user.Timezone,
            user.Locale,
            current.VerifiedBadge,
            current.CreatedAt,
            user.UpdatedAt,
            roles
        );

        return new ProfileUpdateResponse(
            "Profile updated successfully",
            user.UpdatedAt,
            profile
        );
        }); // End of ExecuteAsync
    }


    public async Task<PasswordChangeResult> ChangePasswordAsync(
      Guid userId,
      string? currentPassword,
      string newPassword,
      bool skipCurrentPasswordCheck,
      CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Use a transaction to mirror your SELECT ... FOR UPDATE semantics.
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Load the user (tracked) for update
            var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.UserNotFound, null);
        }

        // Get policy (replace with your own source if different)
        var minLength = await GetPasswordMinimumLengthAsync(cancellationToken);
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < minLength)
        {
            await tx.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.PasswordTooShort, null, minLength);
        }

        // Check current password unless bypassed
        if (!skipCurrentPasswordCheck)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || !VerifyPassword(currentPassword, user.PasswordHash))
            {
                await tx.RollbackAsync(cancellationToken);
                return new PasswordChangeResult(PasswordChangeStatus.InvalidCurrentPassword, null, minLength);
            }
        }

        // Prevent same-as-old
        if (VerifyPassword(newPassword, user.PasswordHash))
        {
            await tx.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.PasswordSame, null, minLength);
        }

        // Update password + timestamps
        var updatedAt = DateTimeOffset.UtcNow;
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = updatedAt;

        await _db.SaveChangesAsync(cancellationToken);

        // Revoke all active refresh tokens for this user (server-side bulk update if on EF Core 7+)
        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(rt => rt.RevokedAt, _ => updatedAt),
                cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var response = new PasswordChangeResponse(
            "Password changed successfully. Please log in again on other devices.",
            updatedAt
        );

        return new PasswordChangeResult(PasswordChangeStatus.Success, response, minLength);
        }); // End of ExecuteAsync
    }


    public async Task<SessionContext?> ResolveCurrentSessionAsync(
        Guid userId,
        Guid? sessionIdClaim,
        string? refreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // 1) From session claim
        if (sessionIdClaim.HasValue)
        {
            var byClaim = await _db.Sessions
                .AsNoTracking()
                .Where(s => s.Id == sessionIdClaim.Value &&
                            s.UserId == userId &&
                            s.RevokedAt == null)
                .Select(s => new SessionContext(s.Id, s.AuthMethod))
                .FirstOrDefaultAsync(cancellationToken);

            if (byClaim is not null) return byClaim;
        }

        // 2) From refresh token hash (most recent valid)
        if (!string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            var byToken = await _db.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.UserId == userId &&
                             rt.TokenHash == refreshTokenHash &&
                             rt.RevokedAt == null &&
                             rt.Session.RevokedAt == null)
                .OrderByDescending(rt => rt.IssuedAt)
                .Select(rt => new SessionContext(rt.SessionId, rt.Session.AuthMethod))
                .FirstOrDefaultAsync(cancellationToken);

            if (byToken is not null) return byToken;
        }

        // 3) From IP + User-Agent (most recent)
        if (!string.IsNullOrWhiteSpace(ipAddress) &&
            IPAddress.TryParse(ipAddress, out var ip) &&
            !string.IsNullOrWhiteSpace(userAgent))
        {
            var byIpUa = await _db.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                            s.RevokedAt == null &&
                            s.IpAddress == ip &&
                            s.UserAgent == userAgent)
                .OrderByDescending(s => s.LastSeenAt)
                .Select(s => new SessionContext(s.Id, s.AuthMethod))
                .FirstOrDefaultAsync(cancellationToken);

            if (byIpUa is not null) return byIpUa;
        }

        // 4) Fallback: most recent active session
        //var recent = await _db.Sessions
        //    .AsNoTracking()
        //    .Where(s => s.UserId == userId && s.RevokedAt == null)
        //    .OrderByDescending(s => s.LastSeenAt)
        //    .Select(s => new SessionContext(s.Id, s.AuthMethod))
        //    .FirstOrDefaultAsync(cancellationToken);

        return null;
    }


    public async Task<SessionsResponse> GetSessionsAsync(
     Guid userId,
     Guid? currentSessionId,
     CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (currentSessionId.HasValue)
        {
            // Update LastSeenAt for the current session only
            await _db.Sessions
                .Where(s => s.Id == currentSessionId.Value && s.UserId == userId)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(s => s.LastSeenAt, _ => now),
                    cancellationToken
                );
        }

        // Fetch active (non-revoked) sessions for the user
        var rows = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .OrderByDescending(s => s.LastSeenAt)
            .Select(s => new
            {
                s.Id,
                s.AuthMethod,
                s.IpAddress,         // Npgsql maps to System.Net.IPAddress
                s.UserAgent,
                s.CreatedAt,
                s.LastSeenAt
            })
            .ToListAsync(cancellationToken);

        // Map to your DTOs (ToString() for IPAddress happens client-side)
        var sessions = rows.Select(r =>
            new SessionInfo(
                r.Id,
                r.AuthMethod,
                r.IpAddress?.ToString() ?? string.Empty,
                r.UserAgent,
                r.CreatedAt,
                r.LastSeenAt,
                currentSessionId.HasValue && r.Id == currentSessionId.Value
            )
        ).ToList();

        return new SessionsResponse(sessions);
    }

    public async Task<SessionLogoutResult> LogoutFromSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Load the session row for this user (tracked -> updateable)
            var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (session is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new SessionLogoutResult(SessionLogoutStatus.NotFound, null);
        }

        if (session.RevokedAt is not null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new SessionLogoutResult(SessionLogoutStatus.AlreadyLoggedOut, null);
        }

        var revokedAt = DateTimeOffset.UtcNow;

        // Revoke the session
        session.RevokedAt = revokedAt;
        await _db.SaveChangesAsync(cancellationToken);

        // Revoke all active refresh tokens tied to this session
        await _db.RefreshTokens
            .Where(rt => rt.SessionId == sessionId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(rt => rt.RevokedAt, _ => revokedAt),
                cancellationToken
            );

        await tx.CommitAsync(cancellationToken);

        var response = new SessionDeleteResponse(
            "Session logged out successfully",
            revokedAt
        );

        return new SessionLogoutResult(SessionLogoutStatus.Success, response);
        }); // End of ExecuteAsync
    }

    public async Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(
       Guid userId,
       Guid? currentSessionId,
       CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            var revokedAt = DateTimeOffset.UtcNow;

        // Revoke other sessions
        var sessionsQuery = _db.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null);

        if (currentSessionId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Id != currentSessionId.Value);

        var affectedSessions = await sessionsQuery
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(s => s.RevokedAt, _ => revokedAt),
                cancellationToken
            );

        // Revoke refresh tokens tied to those sessions (or all if no currentSessionId provided)
        var tokensQuery = _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null);

        if (currentSessionId.HasValue)
            tokensQuery = tokensQuery.Where(rt => rt.SessionId != currentSessionId.Value);

        await tokensQuery
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(rt => rt.RevokedAt, _ => revokedAt),
                cancellationToken
            );

        await tx.CommitAsync(cancellationToken);

        return new SessionDeleteResponse(
            $"Logged out from {affectedSessions} other session(s) successfully",
            revokedAt
        );
        }); // End of ExecuteAsync
    }


    public async Task<AccountDeleteResult> DeleteAccountAsync(
    Guid userId,
    string password,
    CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Load tracked user row (equivalent to SELECT ... FOR UPDATE semantics when used in a tx)
            var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return new AccountDeleteResult(AccountDeleteStatus.UserNotFound, null);
        }

        // Verify current password
        if (!VerifyPassword(password, user.PasswordHash))
        {
            await tx.RollbackAsync(cancellationToken);
            return new AccountDeleteResult(AccountDeleteStatus.InvalidPassword, null);
        }

        var deletedAt = DateTimeOffset.UtcNow;
        var originalEmail = user.Email; // needed for OTP revocation by email
        var anonymizedEmail = $"deleted_{userId}@deleted.local";
        var anonymizedUsername = $"deleted_{userId}";

        // Soft-delete + anonymize user
        user.DeletedAt = deletedAt;
        user.IsActive = false;
        user.Email = anonymizedEmail;
        user.Username = anonymizedUsername;
        user.FirstName = null;
        user.LastName = null;
        user.DisplayName = null;
        user.Bio = null;
        user.AvatarUrl = null;
        user.CoverUrl = null;
        user.UpdatedAt = deletedAt;

        await _db.SaveChangesAsync(cancellationToken);

        // Revoke active sessions
        await _db.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(s => s.RevokedAt, _ => deletedAt),
                cancellationToken);

        // Revoke active refresh tokens
        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(rt => rt.RevokedAt, _ => deletedAt),
                cancellationToken);

        // Consume any outstanding OTPs for the original email
        await _db.OtpCodes
            .Where(o => o.Email == originalEmail && o.ConsumedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(o => o.ConsumedAt, _ => deletedAt),
                cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var response = new AccountDeleteResponse(
            "Account deleted successfully",
            deletedAt
        );

        return new AccountDeleteResult(AccountDeleteStatus.Success, response);
        }); // End of ExecuteAsync
    }


    private async Task<int> GetPasswordMinimumLengthAsync(CancellationToken cancellationToken)
    {
        var value = await _db.AppConfigs
            .AsNoTracking()
            .Where(c => c.Key == "PasswordMinLength")
            .Select(c => c.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (int.TryParse(value, out var parsed))
            return parsed;

        return 0;
    }


    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString(DefaultConnectionName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        const string message = "Database connection string not configured";
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }
}