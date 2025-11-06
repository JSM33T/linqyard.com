namespace Linqyard.Contracts.Responses;

public sealed record ProfileDetailsResponse(
    Guid Id,
    string Email,
    bool EmailVerified,
    string Username,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    string? Timezone,
    string? Locale,
    bool VerifiedBadge,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Roles,
    int? TierId = null,
    string? TierName = null,
    UserTierInfo? ActiveTier = null
);

public sealed record SessionInfo(
    Guid Id,
    string AuthMethod,
    string IpAddress,
    string UserAgent,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    bool IsCurrentSession
);

public sealed record SessionsResponse(
    IReadOnlyList<SessionInfo> Sessions
);

public sealed record ProfileUpdateResponse(
    string Message,
    DateTimeOffset UpdatedAt,
    ProfileDetailsResponse Profile
);

public sealed record PasswordChangeResponse(
    string Message,
    DateTimeOffset ChangedAt
);

public sealed record SessionDeleteResponse(
    string Message,
    DateTimeOffset DeletedAt
);

public sealed record AccountDeleteResponse(
    string Message,
    DateTimeOffset DeletedAt
);

