using System.Security.Cryptography;
using System.Text;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.Extensions.Logging;

namespace Linqyard.Services;

public sealed class ProfileService(IProfileRepository profileRepository, ILogger<ProfileService> logger) : IProfileService
{
    private readonly IProfileRepository _profileRepository = profileRepository;
    private readonly ILogger<ProfileService> _logger = logger;

    public Task<ProfileDetailsResponse?> GetProfileDetailsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _profileRepository.GetProfileDetailsAsync(userId, cancellationToken);

    public async Task<ProfileUpdateResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var usernameCandidate = request.Username?.Trim();
        if (!string.IsNullOrEmpty(usernameCandidate))
        {
            if (usernameCandidate.Length < 3)
            {
                return new ProfileUpdateResult(
                    ProfileUpdateStatus.InvalidUsername,
                    null,
                    "Username must be at least 3 characters long");
            }

            if (usernameCandidate.Length > 30)
            {
                return new ProfileUpdateResult(
                    ProfileUpdateStatus.InvalidUsername,
                    null,
                    "Username cannot exceed 30 characters");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(usernameCandidate, @"^[a-zA-Z0-9_.-]+$"))
            {
                return new ProfileUpdateResult(
                    ProfileUpdateStatus.InvalidUsername,
                    null,
                    "Username can only contain letters, numbers, underscores, dots, and hyphens");
            }
        }

        var displayNameCandidate = request.DisplayName?.Trim();
        if (!string.IsNullOrEmpty(displayNameCandidate) && displayNameCandidate.Length > 50)
        {
            return new ProfileUpdateResult(
                ProfileUpdateStatus.InvalidDisplayName,
                null,
                "Display name cannot exceed 50 characters");
        }

        var bioCandidate = request.Bio?.Trim();
        if (!string.IsNullOrEmpty(bioCandidate) && bioCandidate.Length > 500)
        {
            return new ProfileUpdateResult(
                ProfileUpdateStatus.InvalidBio,
                null,
                "Bio cannot exceed 500 characters");
        }

        try
        {
            var response = await _profileRepository.UpdateProfileAsync(userId, request, cancellationToken);

            return response is null
                ? new ProfileUpdateResult(ProfileUpdateStatus.UserNotFound, null, "User not found")
                : new ProfileUpdateResult(ProfileUpdateStatus.Success, response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Username is already taken")
        {
            _logger.LogWarning(ex, "Username {Username} is already taken for user {UserId}", request.Username, userId);
            return new ProfileUpdateResult(ProfileUpdateStatus.UsernameTaken, null, "Username is already taken");
        }
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await ResolveCurrentSessionAsync(userId, sessionContext, cancellationToken);
        var signedInViaGoogle = currentSession is not null &&
                                string.Equals(currentSession.AuthMethod, "google", StringComparison.OrdinalIgnoreCase);

        return await _profileRepository.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            signedInViaGoogle,
            cancellationToken);
    }

    public async Task<SessionsResponse> GetSessionsAsync(
        Guid userId,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await ResolveCurrentSessionAsync(userId, sessionContext, cancellationToken);
        return await _profileRepository.GetSessionsAsync(userId, currentSession?.Id, cancellationToken);
    }

    public Task<SessionLogoutResult> LogoutFromSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        _profileRepository.LogoutFromSessionAsync(userId, sessionId, cancellationToken);

    public async Task<SessionDeleteResponse> LogoutFromAllOtherSessionsAsync(
        Guid userId,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await ResolveCurrentSessionAsync(userId, sessionContext, cancellationToken);
        return await _profileRepository.LogoutFromAllOtherSessionsAsync(userId, currentSession?.Id, cancellationToken);
    }

    public async Task<AccountDeleteServiceResult> DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.ConfirmationText, "DELETE MY ACCOUNT", StringComparison.Ordinal))
        {
            return new AccountDeleteServiceResult(
                AccountDeleteServiceStatus.InvalidConfirmation,
                null,
                "Confirmation text must be exactly: DELETE MY ACCOUNT");
        }

        var result = await _profileRepository.DeleteAccountAsync(userId, request.Password, cancellationToken);

        return result.Status switch
        {
            AccountDeleteStatus.UserNotFound => new AccountDeleteServiceResult(
                AccountDeleteServiceStatus.UserNotFound,
                null,
                "User not found"),
            AccountDeleteStatus.InvalidPassword => new AccountDeleteServiceResult(
                AccountDeleteServiceStatus.InvalidPassword,
                null,
                "Password is incorrect"),
            AccountDeleteStatus.Success => new AccountDeleteServiceResult(
                AccountDeleteServiceStatus.Success,
                result.Response),
            _ => new AccountDeleteServiceResult(
                AccountDeleteServiceStatus.InvalidConfirmation,
                null,
                "Unable to delete account")
        };
    }

    private async Task<SessionContext?> ResolveCurrentSessionAsync(
        Guid userId,
        ProfileSessionRequest sessionContext,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash = string.IsNullOrWhiteSpace(sessionContext.RefreshToken)
            ? null
            : HashToken(sessionContext.RefreshToken);

        return await _profileRepository.ResolveCurrentSessionAsync(
            userId,
            sessionContext.SessionIdClaim,
            refreshTokenHash,
            sessionContext.IpAddress,
            sessionContext.UserAgent,
            cancellationToken);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
