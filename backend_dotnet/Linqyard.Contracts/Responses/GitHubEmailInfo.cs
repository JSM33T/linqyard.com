namespace Linqyard.Contracts.Responses;

public record GitHubEmailInfo(
    string Email,
    bool Primary,
    bool Verified,
    string? Visibility
);
