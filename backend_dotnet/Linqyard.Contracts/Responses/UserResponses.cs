namespace Linqyard.Contracts.Responses;

public sealed record UserPublicResponse(
    Guid Id,
    string Username,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? CoverUrl,
    string? Bio,
    int? TierId,
    string? TierName
);
