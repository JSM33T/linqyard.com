namespace Linqyard.Contracts.Requests;

public sealed record CreateLinkRequest(
    string Name,
    string Url,
    string? Description = null,
    Guid? GroupId = null,
    int? Sequence = null,
    bool? IsActive = null
);
