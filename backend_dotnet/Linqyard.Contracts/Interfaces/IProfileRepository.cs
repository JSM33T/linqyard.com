using System;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
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

    /// <summary>
    /// Provides persistence operations for user profile data, sessions, and account lifecycle.
    /// </summary>
    public interface IProfileRepository
    {
        /// <summary>
        /// Retrieves profile details for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The profile details or <c>null</c> if not found.</returns>
        Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists profile updates for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="request">Payload describing the changes to apply.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated profile response, or <c>null</c> if not found.</returns>
        Task<ProfileUpdateResponse?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="currentPassword">Current password supplied by the user.</param>
        /// <param name="newPassword">New password to set.</param>
        /// <param name="skipCurrentPasswordCheck">Indicates whether to bypass current password validation.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The outcome of the password change attempt.</returns>
        Task<PasswordChangeResult> ChangePasswordAsync(
            Guid userId,
            string? currentPassword,
            string newPassword,
            bool skipCurrentPasswordCheck,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the current session context for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="sessionIdClaim">Session identifier from the caller's claims.</param>
        /// <param name="refreshTokenHash">Refresh token hash, if present.</param>
        /// <param name="ipAddress">Client IP address.</param>
        /// <param name="userAgent">Client user agent string.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The resolved session context or <c>null</c> if unresolved.</returns>
        Task<SessionContext?> ResolveCurrentSessionAsync(
            Guid userId,
            Guid? sessionIdClaim,
            string? refreshTokenHash,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the active sessions for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="currentSessionId">Identifier of the current session, if known.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The sessions response.</returns>
        Task<SessionsResponse> GetSessionsAsync(
            Guid userId,
            Guid? currentSessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs the user out from a specific session.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="sessionId">Identifier of the session to terminate.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The logout result.</returns>
        Task<SessionLogoutResult> LogoutFromSessionAsync(
            Guid userId,
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs the user out from all sessions except the current one.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="currentSessionId">Identifier of the current session, if any.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The response describing removed sessions.</returns>
        Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(
            Guid userId,
            Guid? currentSessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the user's account permanently.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="password">Password confirmation for the delete.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The outcome of the account delete request.</returns>
        Task<AccountDeleteResult> DeleteAccountAsync(
            Guid userId,
            string password,
            CancellationToken cancellationToken = default);
    }
}
