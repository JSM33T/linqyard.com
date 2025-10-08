using System.Net;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Linkyard.Repositories;

public class ProfileRepository : IProfileRepository
{
    private const string DefaultConnectionName = "DefaultConnection";

    private readonly ILogger<ProfileRepository> _logger;
    private readonly IConfiguration _configuration;

    public ProfileRepository(ILogger<ProfileRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string userQuery = @"
            SELECT ""Id"", ""Email"", ""EmailVerified"", ""Username"", ""FirstName"", ""LastName"",
                   ""DisplayName"", ""Bio"", ""AvatarUrl"", ""CoverUrl"", ""Timezone"", ""Locale"",
                   ""VerifiedBadge"", ""CreatedAt"", ""UpdatedAt""
            FROM public.""Users""
            WHERE ""Id"" = @userId
            LIMIT 1;";

        await using var userCommand = new NpgsqlCommand(userQuery, connection);
        userCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await userCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var profile = new ProfileDetailsResponse(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetBoolean(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.GetBoolean(12),
            reader.GetFieldValue<DateTimeOffset>(13),
            reader.GetFieldValue<DateTimeOffset>(14),
            Array.Empty<string>());

        await reader.CloseAsync();

        const string rolesQuery = @"
            SELECT r.""Name""
            FROM public.""UserRoles"" ur
            INNER JOIN public.""Roles"" r ON r.""Id"" = ur.""RoleId""
            WHERE ur.""UserId"" = @userId;";

        await using var rolesCommand = new NpgsqlCommand(rolesQuery, connection);
        rolesCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        var roles = new List<string>();
        await using var rolesReader = await rolesCommand.ExecuteReaderAsync(cancellationToken);
        while (await rolesReader.ReadAsync(cancellationToken))
        {
            roles.Add(rolesReader.GetString(0));
        }

        return profile with { Roles = roles };
    }

    public async Task<ProfileUpdateResponse?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectQuery = @"
            SELECT ""Id"", ""Email"", ""EmailVerified"", ""Username"", ""FirstName"", ""LastName"",
                   ""DisplayName"", ""Bio"", ""AvatarUrl"", ""CoverUrl"", ""Timezone"", ""Locale"",
                   ""VerifiedBadge"", ""CreatedAt"", ""UpdatedAt""
            FROM public.""Users""
            WHERE ""Id"" = @userId
            FOR UPDATE;";

        await using var selectCommand = new NpgsqlCommand(selectQuery, connection, transaction);
        selectCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var current = new
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            EmailVerified = reader.GetBoolean(2),
            Username = reader.GetString(3),
            FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
            LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
            DisplayName = reader.IsDBNull(6) ? null : reader.GetString(6),
            Bio = reader.IsDBNull(7) ? null : reader.GetString(7),
            AvatarUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            CoverUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
            Timezone = reader.IsDBNull(10) ? null : reader.GetString(10),
            Locale = reader.IsDBNull(11) ? null : reader.GetString(11),
            VerifiedBadge = reader.GetBoolean(12),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(13)
        };

        await reader.CloseAsync();

        var updatedAt = DateTimeOffset.UtcNow;

        string? Sanitize(string? value) => value == null ? null : (string.IsNullOrWhiteSpace(value) ? null : value.Trim());

        var username = request.Username is null ? current.Username : Sanitize(request.Username);
        var firstName = request.FirstName is null ? current.FirstName : Sanitize(request.FirstName);
        var lastName = request.LastName is null ? current.LastName : Sanitize(request.LastName);
        var displayName = request.DisplayName is null ? current.DisplayName : Sanitize(request.DisplayName);
        var bio = request.Bio is null ? current.Bio : Sanitize(request.Bio);
        var avatarUrl = request.AvatarUrl is null ? current.AvatarUrl : Sanitize(request.AvatarUrl);
        var coverUrl = request.CoverUrl is null ? current.CoverUrl : Sanitize(request.CoverUrl);
        var timezone = request.Timezone is null ? current.Timezone : Sanitize(request.Timezone);
        var locale = request.Locale is null ? current.Locale : Sanitize(request.Locale);

        // Check username uniqueness if it's being changed
        if (username != current.Username && !string.IsNullOrEmpty(username))
        {
            const string checkUsernameQuery = @"
                SELECT COUNT(1) FROM public.""Users""
                WHERE LOWER(""Username"") = LOWER(@username) AND ""Id"" != @userId;";

            await using var checkCommand = new NpgsqlCommand(checkUsernameQuery, connection, transaction);
            checkCommand.Parameters.Add("username", NpgsqlDbType.Text).Value = username;
            checkCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

            var existingCountResult = await checkCommand.ExecuteScalarAsync(cancellationToken);
            var existingCount = existingCountResult as long? ?? 0;
            if (existingCount > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Username is already taken");
            }
        }

        const string updateQuery = @"
            UPDATE public.""Users""
            SET ""Username"" = @username,
                ""FirstName"" = @firstName,
                ""LastName"" = @lastName,
                ""DisplayName"" = @displayName,
                ""Bio"" = @bio,
                ""AvatarUrl"" = @avatarUrl,
                ""CoverUrl"" = @coverUrl,
                ""Timezone"" = @timezone,
                ""Locale"" = @locale,
                ""UpdatedAt"" = @updatedAt
            WHERE ""Id"" = @userId;";

        await using var updateCommand = new NpgsqlCommand(updateQuery, connection, transaction);
        updateCommand.Parameters.Add("username", NpgsqlDbType.Text).Value = username;
        updateCommand.Parameters.Add("firstName", NpgsqlDbType.Text).Value = (object?)firstName ?? DBNull.Value;
        updateCommand.Parameters.Add("lastName", NpgsqlDbType.Text).Value = (object?)lastName ?? DBNull.Value;
        updateCommand.Parameters.Add("displayName", NpgsqlDbType.Text).Value = (object?)displayName ?? DBNull.Value;
        updateCommand.Parameters.Add("bio", NpgsqlDbType.Text).Value = (object?)bio ?? DBNull.Value;
        updateCommand.Parameters.Add("avatarUrl", NpgsqlDbType.Text).Value = (object?)avatarUrl ?? DBNull.Value;
        updateCommand.Parameters.Add("coverUrl", NpgsqlDbType.Text).Value = (object?)coverUrl ?? DBNull.Value;
        updateCommand.Parameters.Add("timezone", NpgsqlDbType.Text).Value = (object?)timezone ?? DBNull.Value;
        updateCommand.Parameters.Add("locale", NpgsqlDbType.Text).Value = (object?)locale ?? DBNull.Value;
        updateCommand.Parameters.Add("updatedAt", NpgsqlDbType.TimestampTz).Value = updatedAt;
        updateCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        const string rolesQuery = @"
            SELECT r.""Name""
            FROM public.""UserRoles"" ur
            INNER JOIN public.""Roles"" r ON r.""Id"" = ur.""RoleId""
            WHERE ur.""UserId"" = @userId;";

        await using var rolesCommand = new NpgsqlCommand(rolesQuery, connection, transaction);
        rolesCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        var roles = new List<string>();
        await using var rolesReader = await rolesCommand.ExecuteReaderAsync(cancellationToken);
        while (await rolesReader.ReadAsync(cancellationToken))
        {
            roles.Add(rolesReader.GetString(0));
        }

        await rolesReader.CloseAsync();

        await transaction.CommitAsync(cancellationToken);

        var profile = new ProfileDetailsResponse(
            current.Id,
            current.Email,
            current.EmailVerified,
            username ?? current.Username,
            firstName,
            lastName,
            displayName,
            bio,
            avatarUrl,
            coverUrl,
            timezone,
            locale,
            current.VerifiedBadge,
            current.CreatedAt,
            updatedAt,
            roles);

        return new ProfileUpdateResponse(
            "Profile updated successfully",
            updatedAt,
            profile);
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(Guid userId, string? currentPassword, string newPassword, bool skipCurrentPasswordCheck, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectUserQuery = @"
            SELECT ""PasswordHash"", ""UpdatedAt""
            FROM public.""Users""
            WHERE ""Id"" = @userId
            FOR UPDATE;";

        await using var selectUserCommand = new NpgsqlCommand(selectUserQuery, connection, transaction);
        selectUserCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await selectUserCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.UserNotFound, null);
        }

        var currentHash = reader.GetString(0);
        await reader.CloseAsync();

        var minLength = await GetPasswordMinimumLengthAsync(connection, transaction, cancellationToken);
        if (newPassword.Length < minLength)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.PasswordTooShort, null, minLength);
        }

        if (!skipCurrentPasswordCheck)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || !VerifyPassword(currentPassword, currentHash))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PasswordChangeResult(PasswordChangeStatus.InvalidCurrentPassword, null, minLength);
            }
        }

        if (VerifyPassword(newPassword, currentHash))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new PasswordChangeResult(PasswordChangeStatus.PasswordSame, null, minLength);
        }

        var newHash = HashPassword(newPassword);
        var updatedAt = DateTimeOffset.UtcNow;

        const string updatePasswordQuery = @"
            UPDATE public.""Users""
            SET ""PasswordHash"" = @passwordHash,
                ""UpdatedAt"" = @updatedAt
            WHERE ""Id"" = @userId;";

        await using var updatePasswordCommand = new NpgsqlCommand(updatePasswordQuery, connection, transaction);
        updatePasswordCommand.Parameters.Add("passwordHash", NpgsqlDbType.Text).Value = newHash;
        updatePasswordCommand.Parameters.Add("updatedAt", NpgsqlDbType.TimestampTz).Value = updatedAt;
        updatePasswordCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await updatePasswordCommand.ExecuteNonQueryAsync(cancellationToken);

        const string revokeTokensQuery = @"
            UPDATE public.""RefreshTokens""
            SET ""RevokedAt"" = @revokedAt
            WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL;";

        await using var revokeTokensCommand = new NpgsqlCommand(revokeTokensQuery, connection, transaction);
        revokeTokensCommand.Parameters.Add("revokedAt", NpgsqlDbType.TimestampTz).Value = updatedAt;
        revokeTokensCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await revokeTokensCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = new PasswordChangeResponse(
            "Password changed successfully. Please log in again on other devices.",
            updatedAt);

        return new PasswordChangeResult(PasswordChangeStatus.Success, response, minLength);
    }

    public async Task<SessionContext?> ResolveCurrentSessionAsync(Guid userId, Guid? sessionIdClaim, string? refreshTokenHash, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        if (sessionIdClaim.HasValue)
        {
            const string claimQuery = @"
                SELECT ""Id"", ""AuthMethod""
                FROM public.""Sessions""
                WHERE ""Id"" = @sessionId AND ""UserId"" = @userId AND ""RevokedAt"" IS NULL
                LIMIT 1;";

            await using var claimCommand = new NpgsqlCommand(claimQuery, connection);
            claimCommand.Parameters.Add("sessionId", NpgsqlDbType.Uuid).Value = sessionIdClaim.Value;
            claimCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

            await using var claimReader = await claimCommand.ExecuteReaderAsync(cancellationToken);
            if (await claimReader.ReadAsync(cancellationToken))
            {
                var context = new SessionContext(claimReader.GetGuid(0), claimReader.GetString(1));
                return context;
            }
            await claimReader.CloseAsync();
        }

        if (!string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            const string tokenQuery = @"
                SELECT s.""Id"", s.""AuthMethod""
                FROM public.""RefreshTokens"" rt
                INNER JOIN public.""Sessions"" s ON s.""Id"" = rt.""SessionId""
                WHERE rt.""UserId"" = @userId
                  AND rt.""TokenHash"" = @tokenHash
                  AND rt.""RevokedAt"" IS NULL
                  AND s.""RevokedAt"" IS NULL
                ORDER BY rt.""IssuedAt"" DESC
                LIMIT 1;";

            await using var tokenCommand = new NpgsqlCommand(tokenQuery, connection);
            tokenCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
            tokenCommand.Parameters.Add("tokenHash", NpgsqlDbType.Text).Value = refreshTokenHash;

            await using var tokenReader = await tokenCommand.ExecuteReaderAsync(cancellationToken);
            if (await tokenReader.ReadAsync(cancellationToken))
            {
                return new SessionContext(tokenReader.GetGuid(0), tokenReader.GetString(1));
            }
            await tokenReader.CloseAsync();
        }

        if (!string.IsNullOrWhiteSpace(ipAddress) && IPAddress.TryParse(ipAddress, out var ip) && !string.IsNullOrWhiteSpace(userAgent))
        {
            const string ipQuery = @"
                SELECT ""Id"", ""AuthMethod""
                FROM public.""Sessions""
                WHERE ""UserId"" = @userId
                  AND ""RevokedAt"" IS NULL
                  AND ""IpAddress"" = @ip
                  AND ""UserAgent"" = @userAgent
                ORDER BY ""LastSeenAt"" DESC
                LIMIT 1;";

            await using var ipCommand = new NpgsqlCommand(ipQuery, connection);
            ipCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
            ipCommand.Parameters.Add("ip", NpgsqlDbType.Inet).Value = ip;
            ipCommand.Parameters.Add("userAgent", NpgsqlDbType.Text).Value = userAgent;

            await using var ipReader = await ipCommand.ExecuteReaderAsync(cancellationToken);
            if (await ipReader.ReadAsync(cancellationToken))
            {
                return new SessionContext(ipReader.GetGuid(0), ipReader.GetString(1));
            }
            await ipReader.CloseAsync();
        }

        const string recentQuery = @"
            SELECT ""Id"", ""AuthMethod""
            FROM public.""Sessions""
            WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL
            ORDER BY ""LastSeenAt"" DESC
            LIMIT 1;";

        await using var recentCommand = new NpgsqlCommand(recentQuery, connection);
        recentCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var recentReader = await recentCommand.ExecuteReaderAsync(cancellationToken);
        if (await recentReader.ReadAsync(cancellationToken))
        {
            return new SessionContext(recentReader.GetGuid(0), recentReader.GetString(1));
        }

        return null;
    }

    public async Task<SessionsResponse> GetSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (currentSessionId.HasValue)
        {
            const string updateLastSeenQuery = @"
                UPDATE public.""Sessions""
                SET ""LastSeenAt"" = @now
                WHERE ""Id"" = @sessionId;";

            await using var updateLastSeenCommand = new NpgsqlCommand(updateLastSeenQuery, connection, transaction);
            updateLastSeenCommand.Parameters.Add("now", NpgsqlDbType.TimestampTz).Value = DateTimeOffset.UtcNow;
            updateLastSeenCommand.Parameters.Add("sessionId", NpgsqlDbType.Uuid).Value = currentSessionId.Value;
            await updateLastSeenCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sessionsQuery = @"
            SELECT ""Id"", ""AuthMethod"", ""IpAddress"", ""UserAgent"", ""CreatedAt"", ""LastSeenAt""
            FROM public.""Sessions""
            WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL
            ORDER BY ""LastSeenAt"" DESC;";

        await using var sessionsCommand = new NpgsqlCommand(sessionsQuery, connection, transaction);
        sessionsCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        var sessions = new List<SessionInfo>();
        await using var reader = await sessionsCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var ip = reader.GetFieldValue<IPAddress>(2).ToString();
            var session = new SessionInfo(
                id,
                reader.GetString(1),
                ip,
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                currentSessionId.HasValue && currentSessionId.Value == id);
            sessions.Add(session);
        }

        // Ensure the reader is closed before committing the transaction so
        // we don't attempt another command on the same connection while a
        // reader is still open (which causes NpgsqlOperationInProgressException).
        await reader.CloseAsync();

        await transaction.CommitAsync(cancellationToken);

        return new SessionsResponse(sessions);
    }

    public async Task<SessionLogoutResult> LogoutFromSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectSessionQuery = @"
            SELECT ""RevokedAt""
            FROM public.""Sessions""
            WHERE ""Id"" = @sessionId AND ""UserId"" = @userId
            FOR UPDATE;";

        await using var selectSessionCommand = new NpgsqlCommand(selectSessionQuery, connection, transaction);
        selectSessionCommand.Parameters.Add("sessionId", NpgsqlDbType.Uuid).Value = sessionId;
        selectSessionCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await selectSessionCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return new SessionLogoutResult(SessionLogoutStatus.NotFound, null);
        }

        if (!reader.IsDBNull(0))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return new SessionLogoutResult(SessionLogoutStatus.AlreadyLoggedOut, null);
        }

        await reader.CloseAsync();

        var revokedAt = DateTimeOffset.UtcNow;

        const string revokeSessionQuery = @"
            UPDATE public.""Sessions""
            SET ""RevokedAt"" = @revokedAt
            WHERE ""Id"" = @sessionId;";

        await using var revokeSessionCommand = new NpgsqlCommand(revokeSessionQuery, connection, transaction);
        revokeSessionCommand.Parameters.Add("revokedAt", NpgsqlDbType.TimestampTz).Value = revokedAt;
        revokeSessionCommand.Parameters.Add("sessionId", NpgsqlDbType.Uuid).Value = sessionId;
        await revokeSessionCommand.ExecuteNonQueryAsync(cancellationToken);

        const string revokeTokensQuery = @"
            UPDATE public.""RefreshTokens""
            SET ""RevokedAt"" = @revokedAt
            WHERE ""SessionId"" = @sessionId AND ""RevokedAt"" IS NULL;";

        await using var revokeTokensCommand = new NpgsqlCommand(revokeTokensQuery, connection, transaction);
        revokeTokensCommand.Parameters.Add("revokedAt", NpgsqlDbType.TimestampTz).Value = revokedAt;
        revokeTokensCommand.Parameters.Add("sessionId", NpgsqlDbType.Uuid).Value = sessionId;
        await revokeTokensCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = new SessionDeleteResponse(
            "Session logged out successfully",
            revokedAt);

        return new SessionLogoutResult(SessionLogoutStatus.Success, response);
    }

    public async Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var revokedAt = DateTimeOffset.UtcNow;

        var sessionQuery = currentSessionId.HasValue
            ? @"
                UPDATE public.""Sessions""
                SET ""RevokedAt"" = @revokedAt
                WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL AND ""Id"" <> @currentSessionId;"
            : @"
                UPDATE public.""Sessions""
                SET ""RevokedAt"" = @revokedAt
                WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL;";

        await using var revokeSessionsCommand = new NpgsqlCommand(sessionQuery, connection, transaction);
        revokeSessionsCommand.Parameters.Add("revokedAt", NpgsqlDbType.TimestampTz).Value = revokedAt;
        revokeSessionsCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        if (currentSessionId.HasValue)
        {
            revokeSessionsCommand.Parameters.Add("currentSessionId", NpgsqlDbType.Uuid).Value = currentSessionId.Value;
        }

        var affectedSessions = await revokeSessionsCommand.ExecuteNonQueryAsync(cancellationToken);

        var tokensQuery = currentSessionId.HasValue
            ? @"
                UPDATE public.""RefreshTokens""
                SET ""RevokedAt"" = @revokedAt
                WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL AND ""SessionId"" <> @currentSessionId;"
            : @"
                UPDATE public.""RefreshTokens""
                SET ""RevokedAt"" = @revokedAt
                WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL;";

        await using var revokeTokensCommand = new NpgsqlCommand(tokensQuery, connection, transaction);
        revokeTokensCommand.Parameters.Add("revokedAt", NpgsqlDbType.TimestampTz).Value = revokedAt;
        revokeTokensCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        if (currentSessionId.HasValue)
        {
            revokeTokensCommand.Parameters.Add("currentSessionId", NpgsqlDbType.Uuid).Value = currentSessionId.Value;
        }

        await revokeTokensCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = new SessionDeleteResponse(
            $"Logged out from {affectedSessions} other session(s) successfully",
            revokedAt);

        return response;
    }

    public async Task<AccountDeleteResult> DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectUserQuery = @"
            SELECT ""Email"", ""PasswordHash""
            FROM public.""Users""
            WHERE ""Id"" = @userId
            FOR UPDATE;";

        await using var selectUserCommand = new NpgsqlCommand(selectUserQuery, connection, transaction);
        selectUserCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await selectUserCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return new AccountDeleteResult(AccountDeleteStatus.UserNotFound, null);
        }

        var email = reader.GetString(0);
        var passwordHash = reader.GetString(1);
        await reader.CloseAsync();

        if (!VerifyPassword(password, passwordHash))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountDeleteResult(AccountDeleteStatus.InvalidPassword, null);
        }

        var deletedAt = DateTimeOffset.UtcNow;
        var anonymizedEmail = $"deleted_{userId}@deleted.local";
        var anonymizedUsername = $"deleted_{userId}";

        const string updateUserQuery = @"
            UPDATE public.""Users""
            SET ""DeletedAt"" = @deletedAt,
                ""IsActive"" = FALSE,
                ""Email"" = @email,
                ""Username"" = @username,
                ""FirstName"" = NULL,
                ""LastName"" = NULL,
                ""DisplayName"" = NULL,
                ""Bio"" = NULL,
                ""AvatarUrl"" = NULL,
                ""CoverUrl"" = NULL,
                ""UpdatedAt"" = @deletedAt
            WHERE ""Id"" = @userId;";

        await using var updateUserCommand = new NpgsqlCommand(updateUserQuery, connection, transaction);
        updateUserCommand.Parameters.Add("deletedAt", NpgsqlDbType.TimestampTz).Value = deletedAt;
        updateUserCommand.Parameters.Add("email", NpgsqlDbType.Text).Value = anonymizedEmail;
        updateUserCommand.Parameters.Add("username", NpgsqlDbType.Text).Value = anonymizedUsername;
        updateUserCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);

        const string revokeSessionsQuery = @"
            UPDATE public.""Sessions""
            SET ""RevokedAt"" = @deletedAt
            WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL;";

        await using var revokeSessionsCommand = new NpgsqlCommand(revokeSessionsQuery, connection, transaction);
        revokeSessionsCommand.Parameters.Add("deletedAt", NpgsqlDbType.TimestampTz).Value = deletedAt;
        revokeSessionsCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        await revokeSessionsCommand.ExecuteNonQueryAsync(cancellationToken);

        const string revokeTokensQuery = @"
            UPDATE public.""RefreshTokens""
            SET ""RevokedAt"" = @deletedAt
            WHERE ""UserId"" = @userId AND ""RevokedAt"" IS NULL;";

        await using var revokeTokensCommand = new NpgsqlCommand(revokeTokensQuery, connection, transaction);
        revokeTokensCommand.Parameters.Add("deletedAt", NpgsqlDbType.TimestampTz).Value = deletedAt;
        revokeTokensCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        await revokeTokensCommand.ExecuteNonQueryAsync(cancellationToken);

        const string revokeOtpQuery = @"
            UPDATE public.""OtpCodes""
            SET ""ConsumedAt"" = @deletedAt
            WHERE ""Email"" = @originalEmail AND ""ConsumedAt"" IS NULL;";

        await using var revokeOtpCommand = new NpgsqlCommand(revokeOtpQuery, connection, transaction);
        revokeOtpCommand.Parameters.Add("deletedAt", NpgsqlDbType.TimestampTz).Value = deletedAt;
        revokeOtpCommand.Parameters.Add("originalEmail", NpgsqlDbType.Text).Value = email;
        await revokeOtpCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = new AccountDeleteResponse(
            "Account deleted successfully",
            deletedAt);

        return new AccountDeleteResult(AccountDeleteStatus.Success, response);
    }

    private async Task<int> GetPasswordMinimumLengthAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string minQuery = @"
            SELECT ""Value""
            FROM public.""AppConfigs""
            WHERE ""Key"" = 'PasswordMinLength'
            LIMIT 1;";

        await using var minCommand = new NpgsqlCommand(minQuery, connection, transaction);
        var result = await minCommand.ExecuteScalarAsync(cancellationToken);
        if (result is string value && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

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








