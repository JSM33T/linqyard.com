using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

/// <summary>
/// Encapsulates the HTTP/session context details required to resolve the current session.
/// </summary>
/// <param name="SessionIdClaim">Session identifier resolved from access token claims.</param>
/// <param name="RefreshToken">Raw refresh token value from client cookies (if any).</param>
/// <param name="IpAddress">Client IP address used for auditing.</param>
/// <param name="UserAgent">Client user agent string.</param>
public sealed record ProfileSessionRequest(
    Guid? SessionIdClaim,
    string? RefreshToken,
    string? IpAddress,
    string? UserAgent
);

public enum ProfileUpdateStatus
{
    Success,
    UserNotFound,
    InvalidUsername,
    InvalidDisplayName,
    InvalidBio,
    UsernameTaken
}

public sealed record ProfileUpdateResult(
    ProfileUpdateStatus Status,
    ProfileUpdateResponse? Response,
    string? ErrorMessage = null
);

public enum AccountDeleteServiceStatus
{
    Success,
    InvalidConfirmation,
    UserNotFound,
    InvalidPassword
}

public sealed record AccountDeleteServiceResult(
    AccountDeleteServiceStatus Status,
    AccountDeleteResponse? Response,
    string? ErrorMessage = null
);

public interface IProfileService
{
    Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ProfileUpdateResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default);

    Task<SessionsResponse> GetSessionsAsync(
        Guid userId,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default);

    Task<SessionLogoutResult> LogoutFromSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(
        Guid userId,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default);

    Task<AccountDeleteServiceResult> DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default);
}
