namespace Linqyard.Contracts.Responses;

public sealed record LinkSummary(
    Guid Id,
    string Name,
    string Url,
    string? Description,
    bool IsActive,
    int Sequence,
    Guid? GroupId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record LinkGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    int Sequence,
    bool IsActive,
    IReadOnlyList<LinkSummary> Links
);

public sealed record LinksGroupedResponse(
    IReadOnlyList<LinkGroupResponse> Groups,
    LinkGroupResponse Ungrouped
);
