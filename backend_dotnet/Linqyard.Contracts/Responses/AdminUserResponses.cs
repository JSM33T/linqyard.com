using System;
using System.Collections.Generic;

namespace Linqyard.Contracts.Responses;

public sealed record AdminUserListItemResponse(
    Guid Id,
    string Email,
    bool EmailVerified,
    string Username,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    bool VerifiedBadge,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    UserTierInfo? ActiveTier,
    IReadOnlyList<string> Roles
);

public sealed record AdminUserDetailsResponse(
    ProfileDetailsResponse Profile,
    UserTierInfo? ActiveTier,
    IReadOnlyList<AdminUserTierAssignmentResponse> TierHistory,
    bool IsActive
);

public sealed record AdminUserTierAssignmentResponse(
    Guid AssignmentId,
    int TierId,
    string TierName,
    DateTimeOffset ActiveFrom,
    DateTimeOffset? ActiveUntil,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
