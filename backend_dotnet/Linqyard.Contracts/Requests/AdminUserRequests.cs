using System;
using System.Collections.Generic;

namespace Linqyard.Contracts.Requests;

public sealed record AdminUpdateUserRequest(
    string? Email = null,
    bool? EmailVerified = null,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    string? Bio = null,
    string? AvatarUrl = null,
    string? CoverUrl = null,
    string? Timezone = null,
    string? Locale = null,
    bool? VerifiedBadge = null,
    bool? IsActive = null,
    IReadOnlyList<string>? Roles = null
);

public sealed record AdminUpgradeUserTierRequest(
    int TierId,
    DateTimeOffset? ActiveFrom = null,
    DateTimeOffset? ActiveUntil = null,
    string? Notes = null
);
