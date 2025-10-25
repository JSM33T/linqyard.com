using System;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
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

    /// <summary>
    /// Provides profile management operations for authenticated users and their sessions.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Retrieves the full profile details for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The profile details or <c>null</c> if the user does not exist.</returns>
        Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies profile updates submitted by the user.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="request">The requested changes to apply to the profile.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The outcome of the update request, including validation errors.</returns>
        Task<ProfileUpdateResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the user's password after validating the current session.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="request">The password change payload.</param>
        /// <param name="sessionContext">Session context used to validate the caller.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The result of the password change request.</returns>
        Task<PasswordChangeResult> ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequest request,
            ProfileSessionRequest sessionContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the active sessions associated with a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="sessionContext">Session context used to validate the caller.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A collection of active sessions for the user.</returns>
        Task<SessionsResponse> GetSessionsAsync(
            Guid userId,
            ProfileSessionRequest sessionContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs the user out from a specific session.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="sessionId">The session identifier to terminate.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The result of the logout attempt.</returns>
        Task<SessionLogoutResult> LogoutFromSessionAsync(
            Guid userId,
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs the user out from every other session except the current one.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="sessionContext">Session context used to validate the caller.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The response describing which sessions were removed.</returns>
        Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(
            Guid userId,
            ProfileSessionRequest sessionContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes the user's account and related profile data.
        /// </summary>
        /// <param name="userId">The unique identifier of the profile owner.</param>
        /// <param name="request">The delete-account confirmation payload.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The outcome of the account deletion flow.</returns>
        Task<AccountDeleteServiceResult> DeleteAccountAsync(
            Guid userId,
            DeleteAccountRequest request,
            CancellationToken cancellationToken = default);
    }
}
