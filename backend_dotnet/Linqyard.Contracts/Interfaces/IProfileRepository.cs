using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public record SessionContext(Guid Id, string AuthMethod);

public enum PasswordChangeStatus
{
    Success,
    UserNotFound,
    InvalidCurrentPassword,
    PasswordTooShort,
    PasswordSame
}

public record PasswordChangeResult(PasswordChangeStatus Status, PasswordChangeResponse? Response, int? MinimumLength = null);

public enum SessionLogoutStatus
{
    Success,
    NotFound,
    AlreadyLoggedOut
}

public record SessionLogoutResult(SessionLogoutStatus Status, SessionDeleteResponse? Response);

public enum AccountDeleteStatus
{
    Success,
    UserNotFound,
    InvalidPassword
}

public record AccountDeleteResult(AccountDeleteStatus Status, AccountDeleteResponse? Response);

public interface IProfileRepository
{
    Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileUpdateResponse?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<PasswordChangeResult> ChangePasswordAsync(Guid userId, string? currentPassword, string newPassword, bool skipCurrentPasswordCheck, CancellationToken cancellationToken = default);
    Task<SessionContext?> ResolveCurrentSessionAsync(Guid userId, Guid? sessionIdClaim, string? refreshTokenHash, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<SessionsResponse> GetSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default);
    Task<SessionLogoutResult> LogoutFromSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default);
    Task<AccountDeleteResult> DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
