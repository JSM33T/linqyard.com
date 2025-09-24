using BCrypt.Net;
using FrenCircle.Api.Configuration;
using FrenCircle.Api.Data;
using FrenCircle.Api.Services;
using FrenCircle.Contracts;
using FrenCircle.Contracts.Requests;
using FrenCircle.Contracts.Responses;
using FrenCircle.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FrenCircle.Api.Controllers;

[Route("auth")]
public sealed class AuthController : BaseApiController
{
    private readonly ILogger<AuthController> _logger;
    private readonly FrenCircleDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly HttpClient _httpClient;
    private readonly FrenCircle.Infra.IEmailService _emailService;

    public AuthController(
        ILogger<AuthController> logger, 
        FrenCircleDbContext context,
        IConfiguration configuration,
        IJwtService jwtService,
        HttpClient httpClient,
        FrenCircle.Infra.IEmailService emailService)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _jwtService = jwtService;
        _httpClient = httpClient;
        _emailService = emailService;
    }

    /// <summary>
    /// Authenticate user with email or username and create a new session
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for {EmailOrUsername} with CorrelationId {CorrelationId}", 
            request.EmailOrUsername, CorrelationId);

        try
        {
            // Find user by email or username (case-insensitive)
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || 
                                         u.Username == request.EmailOrUsername, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for {EmailOrUsername}", request.EmailOrUsername);
                return UnauthorizedProblem("Invalid email/username or password");
            }

            // Verify password (in production, use proper password hashing like BCrypt)
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                return UnauthorizedProblem("Invalid email or password");
            }

            // Check if email is verified
            if (!user.EmailVerified)
            {
                _logger.LogWarning("Login failed: Email not verified for user {UserId}", user.Id);
                return UnauthorizedProblem("Please verify your email before logging in");
            }

            // Create session
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AuthMethod = "EmailPassword",
                UserAgent = Request.Headers.UserAgent.ToString() ?? "Unknown",
                IpAddress = HttpContext.Connection.RemoteIpAddress ?? System.Net.IPAddress.Loopback,
                CreatedAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            };

            _context.Sessions.Add(session);

            // Create refresh token
            var refreshTokenValue = GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SessionId = session.Id,
                TokenHash = HashToken(refreshTokenValue), // Store hash, not plain token
                FamilyId = Guid.NewGuid(),
                ExpiresAt = request.RememberMe 
                    ? DateTimeOffset.UtcNow.AddDays(60) 
                    : DateTimeOffset.UtcNow.AddDays(14),
                IssuedAt = DateTimeOffset.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Set refresh token as httpOnly cookie (secure storage)
            // In development over HTTP we fall back to SameSite=Lax and Secure=false so the cookie
            // can be set locally. In production (HTTPS) we use SameSite=None and Secure=true for cross-site requests.
            var loginCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps ? true : false,
                SameSite = HttpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/auth",
                MaxAge = request.RememberMe
                    ? TimeSpan.FromDays(60)
                    : TimeSpan.FromDays(14),
            };

            Response.Cookies.Append("refreshToken", refreshTokenValue, loginCookieOptions);

            // Generate JWT access token
            var accessToken = _jwtService.GenerateToken(user);
            var jwtExpiryMinutes = _configuration.GetSection("JWT:ExpiryMinutes").Get<int?>();
            var expiryMinutes = jwtExpiryMinutes ?? 15;
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

            var userInfo = new UserInfo(
                Id: user.Id,
                Email: user.Email,
                EmailVerified: user.EmailVerified,
                Username: user.Username,
                FirstName: user.FirstName,
                LastName: user.LastName,
                AvatarUrl: user.AvatarUrl,
                CreatedAt: user.CreatedAt,
                Roles: user.UserRoles.Select(ur => ur.Role.Name).ToList()
            );

            // Don't return refresh token in response - it's now in httpOnly cookie
            var authResponse = new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: null, // Removed - now stored securely in httpOnly cookie
                ExpiresAt: expiresAt,
                User: userInfo
            );

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return OkEnvelope(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {EmailOrUsername}", request.EmailOrUsername);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Register a new user account with mandatory username
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registration attempt for email {Email} with CorrelationId {CorrelationId}", 
            request.Email, CorrelationId);

        try
        {
            // Check if signup is disabled
            var signupDisabled = await _context.AppConfigs
                .Where(ac => ac.Key == "SignupDisabled")
                .Select(ac => ac.Value == "true")
                .FirstOrDefaultAsync(cancellationToken);

            if (signupDisabled)
            {
                return BadRequestProblem("User registration is currently disabled");
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser)
            {
                return ConflictProblem("A user with this email already exists");
            }

            // Validate username requirements
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequestProblem("Username is required");
            }

            if (request.Username.Length < 3)
            {
                return BadRequestProblem("Username must be at least 3 characters long");
            }

            if (request.Username.Length > 30)
            {
                return BadRequestProblem("Username cannot be longer than 30 characters");
            }

            // Check if username contains only valid characters (alphanumeric, underscore, hyphen)
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequestProblem("Username can only contain letters, numbers, underscores, and hyphens");
            }

            // Check if username is taken (case-insensitive)
            var existingUsername = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

            if (existingUsername)
            {
                return ConflictProblem("This username is already taken");
            }

            // Validate password requirements
            var minPasswordLength = await _context.AppConfigs
                .Where(ac => ac.Key == "PasswordMinLength")
                .Select(ac => int.Parse(ac.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (request.Password.Length < minPasswordLength)
            {
                return BadRequestProblem($"Password must be at least {minPasswordLength} characters long");
            }

            // Create user (with profile fields consolidated)
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                EmailVerified = false,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Users.Add(user);

            // Assign default user role
            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "user", cancellationToken);

            if (userRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = userRole.Id
                });
            }

            // Create email verification token
            var verificationToken = GenerateVerificationToken();
            var otpCode = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                CodeHash = verificationToken, // Store plain token for development
                Purpose = "Signup",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Verification token generated for {Email}: {Token}", request.Email, verificationToken);
            _context.OtpCodes.Add(otpCode);

            await _context.SaveChangesAsync(cancellationToken);

            // Send verification email
            try
            {
                await _emailService.SendVerificationEmailAsync(request.Email, request.FirstName ?? "User", verificationToken);
                _logger.LogInformation("Verification email sent to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
                // Continue without failing the registration - user can resend verification
            }

            var userInfo = new UserInfo(
                Id: user.Id,
                Email: user.Email,
                EmailVerified: user.EmailVerified,
                Username: user.Username,
                FirstName: user.FirstName,
                LastName: user.LastName,
                AvatarUrl: user.AvatarUrl,
                CreatedAt: user.CreatedAt,
                Roles: userRole != null ? [userRole.Name] : []
            );

            _logger.LogInformation("User {UserId} registered successfully", user.Id);
            
            // TODO: Send verification email with token
            
            return Created($"/auth/users/{user.Id}", new ApiResponse<UserInfo>(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Refresh access token using refresh token from httpOnly cookie
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            // Read refresh token from httpOnly cookie
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue) || 
                string.IsNullOrEmpty(refreshTokenValue))
            {
                _logger.LogWarning("Refresh token cookie not found or empty");
                return UnauthorizedProblem("Refresh token not found");
            }

            var tokenHash = HashToken(refreshTokenValue);
            
            // Find the refresh token by hash
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(rt => rt.Session)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found");
                return UnauthorizedProblem("Invalid refresh token");
            }

            // Check if token is expired
            if (refreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Expired refresh token used for user {UserId}", refreshToken.UserId);
                return UnauthorizedProblem("Refresh token has expired");
            }

            // Check if token is revoked
            if (refreshToken.RevokedAt.HasValue)
            {
                _logger.LogWarning("Revoked refresh token used for user {UserId}", refreshToken.UserId);
                return UnauthorizedProblem("Refresh token has been revoked");
            }

            // Create new refresh token (token rotation)
            var newRefreshTokenValue = GenerateRefreshToken();
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = refreshToken.UserId,
                SessionId = refreshToken.SessionId,
                TokenHash = HashToken(newRefreshTokenValue),
                FamilyId = refreshToken.FamilyId,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
                IssuedAt = DateTimeOffset.UtcNow
            };

            _context.RefreshTokens.Add(newRefreshToken);

            // Revoke old token
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.ReplacedById = newRefreshToken.Id;

            // Update session last seen
            if (refreshToken.Session != null)
            {
                refreshToken.Session.LastSeenAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Set new refresh token as httpOnly cookie (token rotation)
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps ? true : false,
                SameSite = HttpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/auth",
                MaxAge = TimeSpan.FromDays(14)
            };

            Response.Cookies.Append("refreshToken", newRefreshTokenValue, refreshCookieOptions);

            // Generate new JWT access token
            var accessToken = _jwtService.GenerateToken(refreshToken.User);
            var jwtExpiryMinutes = _configuration.GetSection("JWT:ExpiryMinutes").Get<int?>();
            var expiryMinutes = jwtExpiryMinutes ?? 15;
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

            // Don't return refresh token in response - it's in httpOnly cookie
            var response = new RefreshTokenResponse(
                AccessToken: accessToken,
                RefreshToken: null, // Removed - now stored securely in httpOnly cookie
                ExpiresAt: expiresAt
            );

            _logger.LogInformation("Token refreshed successfully for user {UserId}", refreshToken.UserId);
            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Logout user and revoke refresh token from httpOnly cookie
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout attempt with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            // Read refresh token from httpOnly cookie
            if (Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue) && 
                !string.IsNullOrEmpty(refreshTokenValue))
            {
                // Find and revoke refresh token
                var tokenHash = HashToken(refreshTokenValue);
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

                if (refreshToken != null)
                {
                    refreshToken.RevokedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("User {UserId} logged out successfully", refreshToken.UserId);
                }
            }

            // Clear the refresh token cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/auth",
                SameSite = HttpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax
            });

            var response = new LogoutResponse(
                Message: "Logged out successfully",
                LoggedOutAt: DateTimeOffset.UtcNow
            );

            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Verify email with verification token
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<VerificationSentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email verification attempt for {Email} with CorrelationId {CorrelationId}", 
            request.Email, CorrelationId);

        try
        {
            // Find verification token (comparing plain tokens for development)
            var otpCode = await _context.OtpCodes
                .FirstOrDefaultAsync(oc => oc.Email == request.Email && 
                                         oc.CodeHash == request.Token.ToUpper() && 
                                         oc.Purpose == "Signup", 
                                   cancellationToken);

            if (otpCode == null)
            {
                return BadRequestProblem("Invalid verification token");
            }

            if (otpCode.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return BadRequestProblem("Verification token has expired");
            }

            if (otpCode.ConsumedAt.HasValue)
            {
                return BadRequestProblem("Verification token has already been used");
            }

            // Find user and verify email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return BadRequestProblem("User not found");
            }

            user.EmailVerified = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Mark token as used
            otpCode.ConsumedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName ?? "User");
                _logger.LogInformation("Welcome email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                // Continue without failing - email verification was successful
            }

            var response = new VerificationSentResponse(
                Message: "Email verified successfully",
                SentAt: DateTimeOffset.UtcNow
            );

            _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for {Email}", request.Email);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while processing your request");
        }
    }

    // Helper methods (in production, move to separate services)
    private static string HashPassword(string password)
    {
        // Use BCrypt for secure password hashing
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    // Separate method for hashing tokens/codes (still using SHA256 as they're temporary)
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string GenerateVerificationToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..8].ToUpper();
    }

    /// <summary>
    /// Resend email verification code
    /// </summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ApiResponse<VerificationSentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resend verification attempt for {Email} with CorrelationId {CorrelationId}", 
            request.Email, CorrelationId);

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return BadRequestProblem("User not found");
            }

            if (user.EmailVerified)
            {
                return BadRequestProblem("Email is already verified");
            }

            // Generate new verification token
            var verificationToken = GenerateVerificationToken();
            var otpCode = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                CodeHash = verificationToken, // Store plain token for development
                Purpose = "Signup",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New verification code generated for user {UserId}: {Code}", user.Id, verificationToken);

            // Send verification email
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.FirstName ?? "User", verificationToken);
                _logger.LogInformation("Verification email resent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
                // Continue without failing - user can try again
            }

            return Ok(new ApiResponse<VerificationSentResponse>(
                new VerificationSentResponse(
                    "Verification code sent to your email", 
                    DateTimeOffset.UtcNow
                )
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resend verification for {Email}", request.Email);
            return Problem("An error occurred while resending verification");
        }
    }

    /// <summary>
    /// Send password reset code to email
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<VerificationSentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forgot password attempt for {Email} with CorrelationId {CorrelationId}", 
            request.Email, CorrelationId);

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                // Don't reveal if user exists - security best practice
                return Ok(new ApiResponse<VerificationSentResponse>(
                    new VerificationSentResponse(
                        "If this email is registered, you will receive a password reset code", 
                        DateTimeOffset.UtcNow
                    )
                ));
            }

            // Allow password reset for unverified users in case verification email failed
            if (!user.EmailVerified)
            {
                _logger.LogInformation("Password reset requested for unverified email {Email} - allowing in case verification email failed", request.Email);
            }

            // Generate password reset token
            var resetToken = GenerateVerificationToken();
            var otpCode = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                CodeHash = resetToken, // Store plain token for development
                Purpose = "PasswordReset",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset code generated for user {UserId}: {Code}", user.Id, resetToken);

            // Send password reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName ?? "User", resetToken);
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Continue without failing - user can try again
            }

            return Ok(new ApiResponse<VerificationSentResponse>(
                new VerificationSentResponse(
                    "Password reset code sent to your email", 
                    DateTimeOffset.UtcNow
                )
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
            return Problem("An error occurred while processing password reset request");
        }
    }

    /// <summary>
    /// Reset password using verification code
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reset password attempt for {Email} with CorrelationId {CorrelationId}", 
            request.Email, CorrelationId);

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return BadRequestProblem("Invalid reset request");
            }

            // Find valid password reset token (comparing plain tokens for development)
            var otpCode = await _context.OtpCodes
                .FirstOrDefaultAsync(oc => oc.Email == user.Email && 
                                          oc.CodeHash == request.Token.ToUpper() && 
                                          oc.Purpose == "PasswordReset" &&
                                          !oc.ConsumedAt.HasValue, 
                                    cancellationToken);

            if (otpCode == null)
            {
                return BadRequestProblem("Invalid or expired code");
            }

            if (otpCode.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return BadRequestProblem("Reset code has expired");
            }

            // Validate new password
            var minPasswordLength = _configuration.GetValue<int>("Auth:MinPasswordLength", 8);
            if (request.NewPassword.Length < minPasswordLength)
            {
                return BadRequestProblem($"Password must be at least {minPasswordLength} characters long");
            }

            // Update password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // If user's email wasn't verified, verify it now since they proved email access
            var wasEmailUnverified = !user.EmailVerified;
            if (!user.EmailVerified)
            {
                user.EmailVerified = true;
                _logger.LogInformation("Email automatically verified for user {UserId} during password reset", user.Id);
            }

            // Mark reset code as consumed
            otpCode.ConsumedAt = DateTimeOffset.UtcNow;

            // Revoke all existing refresh tokens for security
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.RevokedAt.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successful for user {UserId}", user.Id);

            // Send welcome email if this was the first time email was verified
            if (wasEmailUnverified)
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName ?? "User");
                    _logger.LogInformation("Welcome email sent to newly verified user {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    // Don't fail the password reset for email sending issues
                }
            }

            return Ok(new ApiResponse<object>(new { message = "Password reset successful" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
            return Problem("An error occurred while resetting password");
        }
    }

    /// <summary>
    /// Initiate Google OAuth flow
    /// </summary>
    [HttpGet("google")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GoogleLogin()
    {
        _logger.LogInformation("Initiating Google OAuth flow with CorrelationId {CorrelationId}", CorrelationId);

        var googleSettings = _configuration.GetSection("OAuth:Google").Get<GoogleOAuthSettings>();
        
        if (googleSettings == null || string.IsNullOrEmpty(googleSettings.ClientId))
        {
            _logger.LogError("Google OAuth settings not configured");
            return BadRequestProblem("Google OAuth not configured");
        }

        var state = Guid.NewGuid().ToString();
        
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={Uri.EscapeDataString(googleSettings.ClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(googleSettings.RedirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString("openid profile email")}" +
                      $"&access_type=offline" +
                      $"&state={state}";

        return Ok(new ApiResponse<object>(new { AuthUrl = authUrl }));
    }

    /// <summary>
    /// Handle Google OAuth callback
    /// </summary>
    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleCallback(string code, string state, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Google OAuth callback with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            // Basic validation - in production you might want to store/validate state more securely
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Missing OAuth parameters");
                return BadRequestProblem("Missing OAuth parameters");
            }

            var googleSettings = _configuration.GetSection("OAuth:Google").Get<GoogleOAuthSettings>();
            if (googleSettings == null)
            {
                return BadRequestProblem("Google OAuth not configured");
            }

            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForGoogleToken(code, googleSettings, cancellationToken);
            if (tokenResponse == null)
            {
                return BadRequestProblem("Failed to exchange code for token");
            }

            // Get user info from Google
            var googleUser = await GetGoogleUserInfo(tokenResponse.AccessToken, cancellationToken);
            if (googleUser == null)
            {
                return BadRequestProblem("Failed to get user info from Google");
            }

            // Find or create user
            var user = await FindOrCreateGoogleUser(googleUser, cancellationToken);
            
            // Generate JWT and refresh token
            var authResponse = await GenerateAuthResponse(user, "Google", cancellationToken);

            _logger.LogInformation("Google OAuth login successful for user {UserId}", user.Id);
            return Ok(new ApiResponse<AuthResponse>(authResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google OAuth callback");
            return BadRequestProblem("OAuth authentication failed");
        }
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForGoogleToken(string code, GoogleOAuthSettings settings, CancellationToken cancellationToken)
    {
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", settings.ClientId),
            new KeyValuePair<string, string>("client_secret", settings.ClientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", settings.RedirectUri)
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to exchange code for Google token. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<GoogleTokenResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });
    }

    private async Task<GoogleUserInfo?> GetGoogleUserInfo(string accessToken, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get Google user info. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
    }

    private async Task<User> FindOrCreateGoogleUser(GoogleUserInfo googleUser, CancellationToken cancellationToken)
    {
        // First, check if user exists by external login
        var existingExternalLogin = await _context.ExternalLogins
            .Include(el => el.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(el => el.Provider == "google" && el.ProviderUserId == googleUser.Id, cancellationToken);

        if (existingExternalLogin != null)
        {
            _logger.LogInformation("Found existing Google user {UserId}", existingExternalLogin.User.Id);
            return existingExternalLogin.User;
        }

        // Check if user exists by email
        var existingUser = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email, cancellationToken);

        if (existingUser != null)
        {
            // Link Google account to existing user
            var externalLogin = new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = existingUser.Id,
                Provider = "google",
                ProviderUserId = googleUser.Id,
                ProviderEmail = googleUser.Email,
                LinkedAt = DateTimeOffset.UtcNow
            };

            _context.ExternalLogins.Add(externalLogin);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked Google account to existing user {UserId}", existingUser.Id);
            return existingUser;
        }

        // Create new user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = googleUser.Email,
            EmailVerified = googleUser.VerifiedEmail, // Google already verified the email
            PasswordHash = GenerateRandomHash(), // They won't use password login
            FirstName = googleUser.GivenName,
            LastName = googleUser.FamilyName,
            Username = await GenerateUniqueUsername(googleUser.Name ?? googleUser.Email.Split('@')[0], cancellationToken),
            DisplayName = googleUser.Name,
            AvatarUrl = googleUser.Picture,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(newUser);

        // Add external login record
        var newExternalLogin = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Provider = "google",
            ProviderUserId = googleUser.Id,
            ProviderEmail = googleUser.Email,
            LinkedAt = DateTimeOffset.UtcNow
        };

        _context.ExternalLogins.Add(newExternalLogin);

        // Assign default user role (match the registration pattern)
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user", cancellationToken);
        if (defaultRole != null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = newUser.Id,
                RoleId = defaultRole.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user from Google OAuth {UserId}", newUser.Id);
        return newUser;
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user, string authMethod, CancellationToken cancellationToken)
    {
        // Create session
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AuthMethod = authMethod,
            UserAgent = Request.Headers.UserAgent.ToString() ?? "Unknown",
            IpAddress = HttpContext.Connection.RemoteIpAddress ?? System.Net.IPAddress.Loopback,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };

        _context.Sessions.Add(session);

        // Create refresh token
        var refreshTokenValue = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = HashToken(refreshTokenValue),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IssuedAt = DateTimeOffset.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Generate JWT
        var accessToken = _jwtService.GenerateToken(user);

        // Set refresh token cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAt
        };
        Response.Cookies.Append("refreshToken", refreshTokenValue, cookieOptions);

        var userInfo = new UserInfo(
            user.Id,
            user.Email,
            user.EmailVerified,
            user.Username,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.CreatedAt,
            user.UserRoles.Select(ur => ur.Role.Name).ToArray()
        );

        return new AuthResponse(accessToken, null, DateTimeOffset.UtcNow.AddMinutes(15), userInfo);
    }

    private async Task<string> GenerateUniqueUsername(string baseName, CancellationToken cancellationToken)
    {
        // Clean the base name
        var cleanName = baseName.Replace(" ", "").Replace(".", "").ToLower();
        if (string.IsNullOrEmpty(cleanName)) cleanName = "user";

        var username = cleanName;
        var counter = 1;

        while (await _context.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{cleanName}{counter}";
            counter++;
        }

        return username;
    }

    private static string GenerateRandomHash()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // Google API response models
    private record GoogleTokenResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string TokenType
    );

    private record GoogleUserInfo(
        string Id,
        string Email,
        bool VerifiedEmail,
        string Name,
        string GivenName,
        string FamilyName,
        string Picture,
        string Locale
    );
}