namespace Linqyard.Contracts.Requests;

public sealed record LoginRequest(
    string EmailOrUsername,  // Can be either email or username
    string Password,
    string? DeviceName = null,
    string? DeviceType = null,
    bool RememberMe = false
);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Username,  // Required - user must provide a username
    string? FirstName = null,
    string? LastName = null
);

public sealed record RefreshTokenRequest(
    string RefreshToken
);

public sealed record ForgotPasswordRequest(
    string Email
);

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);

public sealed record VerifyEmailRequest(
    string Email,
    string Token
);

public sealed record ResendVerificationRequest(
    string Email
);

public sealed record SetPasswordRequest(
    string Email,
    string? CurrentPassword,
    string NewPassword
);