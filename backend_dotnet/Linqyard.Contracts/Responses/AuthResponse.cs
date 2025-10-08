namespace Linqyard.Contracts.Responses;

public sealed record AuthResponse(
    string AccessToken,
    string? RefreshToken,  // Returned for frontend compatibility (also stored in httpOnly cookies)
    DateTimeOffset ExpiresAt,
    UserInfo User
);

public sealed record UserInfo(
    Guid Id,
    string Email,
    bool EmailVerified,
    string? Username,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? CoverUrl,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles,
    string? AuthMethod
);

public sealed record RefreshTokenResponse(
    string AccessToken,
    string? RefreshToken,  // Returned for frontend compatibility (also stored in httpOnly cookies)
    DateTimeOffset ExpiresAt
);

public sealed record LogoutResponse(
    string Message,
    DateTimeOffset LoggedOutAt
);

public sealed record VerificationSentResponse(
    string Message,
    DateTimeOffset SentAt
);

public sealed record PasswordResetResponse(
    string Message,
    DateTimeOffset ResetAt
);

