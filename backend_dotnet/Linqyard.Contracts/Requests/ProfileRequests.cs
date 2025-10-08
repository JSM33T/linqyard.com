namespace Linqyard.Contracts.Requests;

public sealed record UpdateProfileRequest(
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    string? Bio = null,
    string? AvatarUrl = null,
    string? CoverUrl = null,
    string? Timezone = null,
    string? Locale = null
);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public sealed record DeleteAccountRequest(
    string Password,
    string ConfirmationText // User must type specific text to confirm deletion
);
