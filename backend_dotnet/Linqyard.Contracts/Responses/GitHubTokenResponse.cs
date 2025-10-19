namespace Linqyard.Contracts.Responses;

public record GitHubTokenResponse(
    string AccessToken,
    string? Scope,
    string TokenType
);
