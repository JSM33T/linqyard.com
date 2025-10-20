namespace Linqyard.Contracts.Requests;

public sealed record CreateGroupRequest(
    string Name,
    string? Description = null,
    int? Sequence = null,
    bool? IsActive = true
);

public sealed record UpdateGroupRequest(
    string? Name = null,
    string? Description = null,
    int? Sequence = null,
    bool? IsActive = null
);

public sealed record GroupResequenceItemRequest(
    Guid Id,
    int Sequence
);
