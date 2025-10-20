namespace Linqyard.Contracts.Responses;

/// <summary>
/// Represents the availability status for a value such as username or email.
/// </summary>
/// <param name="Value">The normalized value that was checked.</param>
/// <param name="IsValid">Whether the supplied value passed local validation checks.</param>
/// <param name="Available">Whether the value is available for use.</param>
/// <param name="Reason">Optional user-facing reason explaining why the value is unavailable or invalid.</param>
/// <param name="ConflictType">
/// Optional machine-readable descriptor when the value conflicts with an existing record
/// (e.g. "VerifiedUser", "PendingVerification").
/// </param>
public sealed record AvailabilityResponse(
    string Value,
    bool IsValid,
    bool Available,
    string? Reason = null,
    string? ConflictType = null);
