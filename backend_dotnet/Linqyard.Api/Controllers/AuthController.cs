using BCrypt.Net;
using Linqyard.Api.Configuration;
using Linqyard.Data;
using Linqyard.Api.Services;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Entities;
using Linqyard.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Linqyard.Api.Extensions;
using Linqyard.Infra;

namespace Linqyard.Api.Controllers;

[Route("auth")]
public sealed class AuthController : BaseApiController
{
    private readonly ILogger<AuthController> _logger;
    private readonly LinqyardDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly HttpClient _httpClient;
    private readonly Linqyard.Infra.IEmailService _emailService;
    private readonly IRateLimiterService _rateLimiter;
    private readonly IAzureBlobStorageService _blobStorageService;
    private const string GitHubUserAgent = "LinqyardApp/1.0";
    private const string ExternalAvatarUserAgent = "LinqyardAvatarFetcher/1.0";

    public AuthController(
        ILogger<AuthController> logger,
        LinqyardDbContext context,
        IConfiguration configuration,
        IJwtService jwtService,
        IRateLimiterService rateLimiter,
        HttpClient httpClient,
        Linqyard.Infra.IEmailService emailService,
        IAzureBlobStorageService blobStorageService)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _jwtService = jwtService;
        _rateLimiter = rateLimiter;
        _httpClient = httpClient;
        _emailService = emailService;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Authenticate user with email or username and create a new session
    /// </summary>
    [HttpPost("login")]
    [RateLimit("auth-login", Partition = RateLimitPartitionStrategy.IpAddress)]
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
                .Include(u => u.UserTiers)
                    .ThenInclude(ut => ut.Tier)
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

            // Create refresh token (use centralized helper so lifetimes are consistent)
            var (refreshTokenValue, refreshToken) = await CreateAndAddRefreshToken(user.Id, session.Id, request.RememberMe);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Generate JWT access token with session ID
            var accessToken = _jwtService.GenerateToken(user, session.Id);
            var jwtExpiryMinutes = _configuration.GetSection("JWT:ExpiryMinutes").Get<int?>();
            // var expiryMinutes = jwtExpiryMinutes ?? 15;
            // var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
            var expiryMinutes = jwtExpiryMinutes ?? 15;
            if (expiryMinutes <= 0 || expiryMinutes > 24 * 60) // 1 day cap, tweak as you like
            {
                _logger.LogError("Invalid JWT:ExpiryMinutes value {Configured}. Falling back to 15.", jwtExpiryMinutes);
                expiryMinutes = 15;
            }
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

            var userInfo = await BuildUserInfoAsync(
                user,
                authMethod: "EmailPassword",
                cancellationToken: cancellationToken);

            // Set refresh token as HTTP-only cookie
            if (!string.IsNullOrEmpty(refreshTokenValue))
            {
                var refreshTokenCookieOptions = CreateSecureCookieOptions(TimeSpan.FromDays(request.RememberMe ? 60 : 14)); // Match refresh token expiry
                Response.Cookies.Append("refreshToken", refreshTokenValue, refreshTokenCookieOptions);
                _logger.LogInformation("Set refresh token HTTP-only cookie for regular login user {UserId}", user.Id);
            }

            // Return refresh token in response for frontend compatibility
            var authResponse = new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshTokenValue, // Return for frontend usage
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
    /// Check whether a username is syntactically valid and currently available.
    /// </summary>
    [HttpGet("availability/username")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckUsernameAvailability([FromQuery] string? username, CancellationToken cancellationToken = default)
    {
        var rawValue = username ?? string.Empty;
        var normalized = rawValue.Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: string.Empty,
                    IsValid: false,
                    Available: false,
                    Reason: "Username is required."
                ));
            }

            if (normalized.Length < 3)
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: false,
                    Available: false,
                    Reason: "Username must be at least 3 characters long."
                ));
            }

            if (normalized.Length > 30)
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: false,
                    Available: false,
                    Reason: "Username cannot be longer than 30 characters."
                ));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-zA-Z0-9_-]+$"))
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: false,
                    Available: false,
                    Reason: "Username can only contain letters, numbers, underscores, and hyphens."
                ));
            }

            var normalizedLower = normalized.ToLowerInvariant();

            var existing = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedLower, cancellationToken);

            if (existing is null)
            {
                _logger.LogInformation("Username {Username} is available", normalized);
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: true,
                    Available: true
                ));
            }

            if (existing.EmailVerified)
            {
                _logger.LogInformation("Username {Username} is taken by a verified account", normalized);
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: true,
                    Available: false,
                    Reason: "This username is already taken.",
                    ConflictType: "VerifiedUser"
                ));
            }

            _logger.LogInformation("Username {Username} is held by an unverified account", normalized);
            return OkEnvelope(new AvailabilityResponse(
                Value: normalized,
                IsValid: true,
                Available: true,
                Reason: "An unverified account currently holds this username. Continuing will replace it.",
                ConflictType: "PendingVerification"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability for {Username}", normalized);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "Could not check username availability at this time.");
        }
    }

    /// <summary>
    /// Check whether an email is syntactically valid and currently available.
    /// </summary>
    [HttpGet("availability/email")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEmailAvailability([FromQuery] string? email, CancellationToken cancellationToken = default)
    {
        var rawValue = email ?? string.Empty;
        var normalized = rawValue.Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: string.Empty,
                    IsValid: false,
                    Available: false,
                    Reason: "Email address is required."
                ));
            }

            if (normalized.Length > 256)
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: false,
                    Available: false,
                    Reason: "Email address is too long."
                ));
            }

            try
            {
                var mailAddress = new MailAddress(normalized);
                normalized = mailAddress.Address;
            }
            catch (FormatException)
            {
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: false,
                    Available: false,
                    Reason: "Please enter a valid email address."
                ));
            }

            var normalizedLower = normalized.ToLowerInvariant();

            var existing = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedLower, cancellationToken);

            if (existing is null)
            {
                _logger.LogInformation("Email {Email} is available", normalized);
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: true,
                    Available: true
                ));
            }

            if (existing.EmailVerified)
            {
                _logger.LogInformation("Email {Email} is already in use by a verified account", normalized);
                return OkEnvelope(new AvailabilityResponse(
                    Value: normalized,
                    IsValid: true,
                    Available: false,
                    Reason: "An account with this email already exists.",
                    ConflictType: "VerifiedUser"
                ));
            }

            _logger.LogInformation("Email {Email} is associated with an unverified account", normalized);
            return OkEnvelope(new AvailabilityResponse(
                Value: normalized,
                IsValid: true,
                Available: true,
                Reason: "An unverified account exists for this email. Continuing will replace it.",
                ConflictType: "PendingVerification"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability for {Email}", normalized);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "Could not check email availability at this time.");
        }
    }

    /// <summary>
    /// Register a new user account with mandatory username
    /// </summary>
    [HttpPost("register")]
    [RateLimit("auth-register", Partition = RateLimitPartitionStrategy.IpAddress)]
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
            var signupDisabled = await _context.AppConfigs.AsNoTracking()
                .Where(ac => ac.Key == "SignupDisabled")
                .Select(ac => ac.Value == "true")
                .FirstOrDefaultAsync(cancellationToken);

            if (signupDisabled)
            {
                return BadRequestProblem("User registration is currently disabled");
            }

            // ── Basic username validation ─────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequestProblem("Username is required");

            if (request.Username.Length < 3)
                return BadRequestProblem("Username must be at least 3 characters long");

            if (request.Username.Length > 30)
                return BadRequestProblem("Username cannot be longer than 30 characters");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
                return BadRequestProblem("Username can only contain letters, numbers, underscores, and hyphens");

            // Password policy
            var minPasswordLength = await _context.AppConfigs.AsNoTracking()
                .Where(ac => ac.Key == "PasswordMinLength")
                .Select(ac => int.Parse(ac.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (request.Password?.Length < minPasswordLength)
                return BadRequestProblem($"Password must be at least {minPasswordLength} characters long");

            // Normalize for case-insensitive checks
            var normalizedEmail = request.Email.Trim();
            var normalizedUsername = request.Username.Trim();
            var normalizedUsernameLower = normalizedUsername.ToLower();

            // ── Look up potential conflicting accounts (TRACKED; we may delete) ──
            var emailOwner = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            var usernameOwner = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsernameLower, cancellationToken);

            // ── Hard conflicts (verified accounts) ────────────────────────────
            if (emailOwner is not null && emailOwner.EmailVerified == true)
            {
                return ConflictProblem("A user with this email already exists");
            }

            if (usernameOwner is not null && usernameOwner.EmailVerified == true)
            {
                return ConflictProblem("This username is already taken");
            }

            // ── Soft conflicts (unverified accounts → reclaim by removing) ────
            // If there’s an unverified account holding the same email, remove it.
            if (emailOwner is not null && emailOwner.EmailVerified == false)
            {
                _logger.LogInformation("Removing unverified user holding email {Email} to allow re-registration", normalizedEmail);
                _context.Users.Remove(emailOwner); // assumes cascade for related rows
                await _context.SaveChangesAsync(cancellationToken);
            }

            // If there’s an unverified account holding the same username, remove it.
            // Note: Re-query in case emailOwner removal also freed the username.
            usernameOwner = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsernameLower, cancellationToken);

            if (usernameOwner is not null && usernameOwner.EmailVerified == false)
            {
                _logger.LogInformation("Removing unverified user holding username {Username} to allow re-registration", normalizedUsername);
                _context.Users.Remove(usernameOwner); // assumes cascade for related rows
                await _context.SaveChangesAsync(cancellationToken);
            }

            // ── Final safety check (should not hit if above handled properly) ──
            var usernameStillTaken = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.Username.ToLower() == normalizedUsernameLower, cancellationToken);
            if (usernameStillTaken)
            {
                return ConflictProblem("This username is already taken");
            }

            // ── Create user ────────────────────────────────────────────────────
            var userId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var freeTier = await _context.Tiers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == (int)TierType.Free, cancellationToken);

            if (freeTier is null)
            {
                _logger.LogError("Default free tier definition is missing. Unable to register user {UserId}", userId);
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Configuration Error",
                    detail: "Unable to complete registration because the default tier is not configured.");
            }
            var user = new User
            {
                Id = userId,
                Email = normalizedEmail,
            PasswordHash = HashPassword(request.Password!),
            EmailVerified = false,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = normalizedUsername,
            CreatedAt = now,
            UpdatedAt = now
        };

            var userTier = new UserTier
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TierId = freeTier.Id,
                ActiveFrom = now,
                ActiveUntil = null,
                IsActive = true,
                Notes = "Assigned during email registration",
                CreatedAt = now,
                UpdatedAt = now
            };

            user.UserTiers.Add(userTier);

            _context.Users.Add(user);
            _context.UserTiers.Add(userTier);

            var activeTierInfo = new UserTierInfo(freeTier.Id, freeTier.Name, now, null);

            // Assign default role (assumes cascade on UserRoles; no AsNoTracking here)
            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "user", cancellationToken);

            if (userRole != null)
            {
                var userRoleLink = new UserRole
                {
                    UserId = userId,
                    RoleId = userRole.Id
                };

                _context.UserRoles.Add(userRoleLink);
                user.UserRoles.Add(userRoleLink);
            }

            // Create email verification token
            var verificationToken = GenerateVerificationToken();
            var otpCode = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                CodeHash = verificationToken, // plain for dev, replace with hash in prod
                Purpose = "Signup",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Verification token generated for {Email}: {Token}", normalizedEmail, verificationToken);
            _context.OtpCodes.Add(otpCode);

            await _context.SaveChangesAsync(cancellationToken);

            // Send verification email (best-effort)
            try
            {
                await _emailService.SendVerificationEmailAsync(normalizedEmail, request.FirstName ?? "User", verificationToken);
                _logger.LogInformation("Verification email sent to {Email}", normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", normalizedEmail);
                // Non-fatal: user can resend verification
            }

            var userInfo = await BuildUserInfoAsync(
                user,
                userRole != null ? new[] { userRole.Name } : Array.Empty<string>(),
                cancellationToken: cancellationToken,
                activeTierOverride: activeTierInfo);

            _logger.LogInformation("User {UserId} registered successfully", user.Id);

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
    /// Refresh access token using refresh token from request body
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            // Read refresh token from request body or HTTP-only cookie
            var refreshTokenValue = request.RefreshToken;

            // Fallback to HTTP-only cookie if not provided in request body
            if (string.IsNullOrEmpty(refreshTokenValue))
            {
                if (Request.Cookies.TryGetValue("refreshToken", out var cookieToken))
                {
                    refreshTokenValue = cookieToken;
                    _logger.LogInformation("Using refresh token from HTTP-only cookie");
                }
            }

            if (string.IsNullOrEmpty(refreshTokenValue))
            {
                _logger.LogWarning("Refresh token not provided in request body or cookie");
                return UnauthorizedProblem("Refresh token not found");
            }

            var tokenHash = HashToken(refreshTokenValue);

            // Find the refresh token by hash
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserTiers)
                        .ThenInclude(ut => ut.Tier)
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
            // Create new refresh token (token rotation) using centralized helper
            var (newRefreshTokenValue, newRefreshToken) = await CreateAndAddRefreshToken(
                refreshToken.UserId,
                refreshToken.SessionId,
                rememberMe: false,
                familyId: refreshToken.FamilyId);

            // Revoke old token
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.ReplacedById = newRefreshToken.Id;

            // Update session last seen
            if (refreshToken.Session != null)
            {
                refreshToken.Session.LastSeenAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Generate new JWT access token with session ID
            var accessToken = _jwtService.GenerateToken(refreshToken.User, refreshToken.SessionId);
            var jwtExpiryMinutes = _configuration.GetSection("JWT:ExpiryMinutes").Get<int?>();
            var expiryMinutes = jwtExpiryMinutes ?? 15;
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

            // Set new refresh token as HTTP-only cookie
            if (!string.IsNullOrEmpty(newRefreshTokenValue))
            {
                var refreshTokenCookieOptions = CreateSecureCookieOptions(newRefreshToken.ExpiresAt - DateTimeOffset.UtcNow);
                Response.Cookies.Append("refreshToken", newRefreshTokenValue, refreshTokenCookieOptions);
            }

            // Return refresh token in response for frontend compatibility
            var response = new RefreshTokenResponse(
                AccessToken: accessToken,
                RefreshToken: newRefreshTokenValue, // Return for frontend usage
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
    /// Logout user and revoke refresh token from request body
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout attempt with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            // If a refresh token is provided, revoke that token and its session (and related tokens)
            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                var tokenHash = HashToken(request.RefreshToken);
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.Session)
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

                if (refreshToken != null)
                {
                    var now = DateTimeOffset.UtcNow;

                    // Revoke the provided refresh token
                    refreshToken.RevokedAt = now;

                    // Revoke the session if present
                    if (refreshToken.Session != null && !refreshToken.Session.RevokedAt.HasValue)
                    {
                        refreshToken.Session.RevokedAt = now;
                    }

                    // Revoke any other refresh tokens tied to the same session
                    if (refreshToken.SessionId != Guid.Empty)
                    {
                        var siblingTokens = await _context.RefreshTokens
                            .Where(rt => rt.SessionId == refreshToken.SessionId && !rt.RevokedAt.HasValue)
                            .ToListAsync(cancellationToken);

                        foreach (var rt in siblingTokens)
                        {
                            rt.RevokedAt = now;
                        }
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("User {UserId} logged out and session revoked", refreshToken.UserId);
                }
            }
            else
            {
                // No refresh token provided - attempt to revoke current session from JWT claim
                var sessionClaim = User.FindFirst("session_id")?.Value ?? User.FindFirst("sid")?.Value;
                if (!string.IsNullOrEmpty(sessionClaim) && Guid.TryParse(sessionClaim, out var sessionId))
                {
                    var session = await _context.Sessions
                        .Include(s => s.RefreshTokens)
                        .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

                    if (session != null)
                    {
                        var now = DateTimeOffset.UtcNow;
                        if (!session.RevokedAt.HasValue)
                        {
                            session.RevokedAt = now;
                        }

                        foreach (var rt in session.RefreshTokens.Where(r => !r.RevokedAt.HasValue))
                        {
                            rt.RevokedAt = now;
                        }

                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Session {SessionId} revoked via logout", sessionId);
                    }
                }
            }

            // Clear refresh token HTTP-only cookie (use same options as when setting)
            var deleteCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = HttpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/" // Match the path used when setting the cookie
            };

            // Set domain for development cross-port access
            var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", false) ||
                               !_configuration.GetValue<bool>("IsProduction", false);
            if (isDevelopment && HttpContext.Request.Host.Host == "localhost")
            {
                deleteCookieOptions.Domain = "localhost";
            }

            Response.Cookies.Delete("refreshToken", deleteCookieOptions);

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

    // Centralized helper to create a refresh token entity and add it to the DbContext.
    // Returns the plain token value (to return to client) and the created RefreshToken entity (for further processing).
    private Task<(string TokenValue, RefreshToken RefreshToken)> CreateAndAddRefreshToken(
        Guid userId,
        Guid sessionId,
        bool rememberMe = false,
        Guid? familyId = null)
    {
        var now = DateTimeOffset.UtcNow;

        // Determine expiry days from configuration, with sensible defaults
        var defaultDays = _configuration.GetValue<int>("Auth:RefreshTokenDays", 14);
        var rememberDays = _configuration.GetValue<int>("Auth:RefreshTokenRememberDays", 60);
        var days = rememberMe ? rememberDays : defaultDays;

        var tokenValue = GenerateRefreshToken();
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            TokenHash = HashToken(tokenValue),
            FamilyId = familyId ?? Guid.NewGuid(),
            ExpiresAt = now.AddDays(days),
            IssuedAt = now
        };

        _context.RefreshTokens.Add(token);

        return Task.FromResult((tokenValue, token));
    }

    private static string GenerateVerificationToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..8].ToUpper();
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userIdString = UserId; // From BaseApiController

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return UnauthorizedProblem("Not authenticated");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTiers)
                .ThenInclude(ut => ut.Tier)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return UnauthorizedProblem("User not found");
        }

        // Determine auth method from session or fallback to external login detection
        string? authMethod = null;

        // Try to get session ID from JWT claims
        var sessionClaim = User.FindFirst("session_id")?.Value ?? User.FindFirst("sid")?.Value;
        if (!string.IsNullOrEmpty(sessionClaim) && Guid.TryParse(sessionClaim, out var sessionId))
        {
            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

            if (session != null)
            {
                authMethod = session.AuthMethod;
            }
        }

        // Fallback: if no session found, check external logins
        if (string.IsNullOrEmpty(authMethod))
        {
            var hasGoogleLogin = await _context.ExternalLogins
                .AsNoTracking()
                .AnyAsync(el => el.UserId == userId && el.Provider == "google", cancellationToken);

            authMethod = hasGoogleLogin ? "Google" : "EmailPassword";
        }

        var userInfo = await BuildUserInfoAsync(
            user,
            authMethod: authMethod,
            cancellationToken: cancellationToken);

        return OkEnvelope(userInfo);
    }

    /// <summary>
    /// Test endpoint to check cookie functionality (development only)
    /// </summary>
    [HttpGet("test-cookies")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult TestCookies()
    {
        var isHttps = HttpContext.Request.IsHttps;
        var cookieValue = Request.Cookies.TryGetValue("refreshToken", out var cookie) ? cookie : "Not found";

        return Ok(new ApiResponse<object>(new
        {
            IsHttps = isHttps,
            RefreshTokenCookie = cookieValue != "Not found" ? "Present" : "Not found",
            AllCookies = Request.Cookies.Keys.ToArray(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Origin = Request.Headers.Origin.ToString()
        }));
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
            var throttleKey = request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(throttleKey))
            {
                return BadRequestProblem("A valid email is required to resend verification.");
            }

            var rateDecision = await _rateLimiter.ShouldAllowAsync("auth-verification-resend", throttleKey, cancellationToken);
            ApplyRateLimitHeaders(rateDecision);
            if (!rateDecision.IsAllowed)
            {
                return TooManyRequestsProblem(
                    "Too many verification requests",
                    "Please wait before requesting another verification email.",
                    rateDecision);
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

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
            var throttleKey = request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(throttleKey))
            {
                return BadRequestProblem("A valid email is required to reset a password.");
            }

            var rateDecision = await _rateLimiter.ShouldAllowAsync("auth-forgot-password", throttleKey, cancellationToken);
            ApplyRateLimitHeaders(rateDecision);
            if (!rateDecision.IsAllowed)
            {
                return TooManyRequestsProblem(
                    "Too many password reset requests",
                    "Please wait before requesting another password reset email.",
                    rateDecision);
            }

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
    /// Set or change password. Google-only accounts can set password without current password.
    /// Other accounts must provide current password.
    /// </summary>
    [HttpPost("set-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPassword([FromBody] Linqyard.Contracts.Requests.SetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetPassword attempt for {Email} with CorrelationId {CorrelationId}", request.Email, CorrelationId);

        try
        {
            var user = await _context.Users
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return BadRequestProblem("User not found");
            }

            var hasExistingPassword = HasUsablePassword(user.PasswordHash);

            if (hasExistingPassword)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return BadRequestProblem("Current password is required");
                }

                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return UnauthorizedProblem("Current password is incorrect");
                }
            }

            // Validate new password length using existing app config fallback
            var minPasswordLength = _configuration.GetValue<int>("Auth:MinPasswordLength", 8);
            if (request.NewPassword.Length < minPasswordLength)
            {
                return BadRequestProblem($"Password must be at least {minPasswordLength} characters long");
            }

            // Update password and revoke existing refresh tokens
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.RevokedAt.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var rt in refreshTokens)
            {
                rt.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password set/changed successfully for user {UserId}", user.Id);

            return Ok(new ApiResponse<object>(new { message = "Password updated successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during set password for {Email}", request.Email);
            return Problem("An error occurred while setting password");
        }
    }

    /// <summary>
    /// Initiate GitHub OAuth flow
    /// </summary>
    [HttpGet("github")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GitHubLogin()
    {
        _logger.LogInformation("Initiating GitHub OAuth flow with CorrelationId {CorrelationId}", CorrelationId);

        var githubSettings = _configuration.GetSection("OAuth:GitHub").Get<GitHubOAuthSettings>();

        if (githubSettings == null || string.IsNullOrEmpty(githubSettings.ClientId))
        {
            _logger.LogError("GitHub OAuth settings not configured");
            return BadRequestProblem("GitHub OAuth not configured");
        }

        var state = Guid.NewGuid().ToString();
        var scope = "read:user user:email";

        var authUrl = "https://github.com/login/oauth/authorize" +
                      $"?client_id={Uri.EscapeDataString(githubSettings.ClientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(githubSettings.RedirectUri)}" +
                      $"&scope={Uri.EscapeDataString(scope)}" +
                      $"&state={state}" +
                      "&allow_signup=true";

        return Ok(new ApiResponse<object>(new { AuthUrl = authUrl }));
    }

    /// <summary>
    /// Handle GitHub OAuth callback
    /// </summary>
    [HttpGet("github/callback")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GitHubCallback(string code, string state, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing GitHub OAuth callback with CorrelationId {CorrelationId}", CorrelationId);

        try
        {
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Missing OAuth parameters for GitHub");
                return BadRequestProblem("Missing OAuth parameters");
            }

            var githubSettings = _configuration.GetSection("OAuth:GitHub").Get<GitHubOAuthSettings>();
            if (githubSettings == null)
            {
                return BadRequestProblem("GitHub OAuth not configured");
            }

            var tokenResponse = await ExchangeCodeForGitHubToken(code, githubSettings, cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return BadRequestProblem("Failed to exchange code for token");
            }

            var githubUser = await GetGitHubUserDetails(tokenResponse.AccessToken, cancellationToken);
            if (githubUser == null)
            {
                return BadRequestProblem("Unable to retrieve required email information from GitHub. Please ensure your account has a verified email address.");
            }

            var user = await FindOrCreateGitHubUser(githubUser, cancellationToken);

            var authResponse = await GenerateAuthResponse(user, "GitHub", cancellationToken);

            _logger.LogInformation("GitHub OAuth login successful for user {UserId}", user.Id);

            if (!string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                var refreshTokenCookieOptions = CreateSecureCookieOptions(TimeSpan.FromDays(7));
                Response.Cookies.Append("refreshToken", authResponse.RefreshToken, refreshTokenCookieOptions);
                _logger.LogInformation("Set refresh token HTTP-only cookie for GitHub OAuth user {UserId}", user.Id);
            }
            else
            {
                _logger.LogWarning("No refresh token available to set as cookie for GitHub user {UserId}", user.Id);
            }

            var frontendUrl = _configuration.GetValue<string>("Frontend:BaseUrl", "http://localhost:3000");
            var redirectUrl = $"{frontendUrl}/account/oauth/callback?success=true&token={authResponse.AccessToken}&expires={authResponse.ExpiresAt:yyyy-MM-ddTHH:mm:ssZ}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub OAuth callback");

            var frontendUrl = _configuration.GetValue<string>("Frontend:BaseUrl", "http://localhost:3000");
            var errorUrl = $"{frontendUrl}/account/oauth/callback?success=false&error=authentication_failed";

            return Redirect(errorUrl);
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

            // Set refresh token as HTTP-only cookie  
            if (!string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                var refreshTokenCookieOptions = CreateSecureCookieOptions(TimeSpan.FromDays(7)); // Match refresh token expiry
                Response.Cookies.Append("refreshToken", authResponse.RefreshToken, refreshTokenCookieOptions);
                _logger.LogInformation("Set refresh token HTTP-only cookie for Google OAuth user {UserId}", user.Id);
            }
            else
            {
                _logger.LogWarning("No refresh token available to set as cookie for user {UserId}", user.Id);
            }

            // Must redirect back to frontend (this is a browser redirect, not an API call)
            var frontendUrl = _configuration.GetValue<string>("Frontend:BaseUrl", "http://localhost:3000");
            var redirectUrl = $"{frontendUrl}/account/oauth/callback?success=true&token={authResponse.AccessToken}&expires={authResponse.ExpiresAt:yyyy-MM-ddTHH:mm:ssZ}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google OAuth callback");

            // Redirect back to frontend with error
            var frontendUrl = _configuration.GetValue<string>("Frontend:BaseUrl", "http://localhost:3000");
            var errorUrl = $"{frontendUrl}/account/oauth/callback?success=false&error=authentication_failed";

            return Redirect(errorUrl);
        }
    }

    private async Task<GitHubTokenResponse?> ExchangeCodeForGitHubToken(string code, GitHubOAuthSettings settings, CancellationToken cancellationToken)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", settings.ClientId),
                new KeyValuePair<string, string>("client_secret", settings.ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", settings.RedirectUri)
            })
        };

        tokenRequest.Headers.Accept.Clear();
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        tokenRequest.Headers.UserAgent.ParseAdd(GitHubUserAgent);

        var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to exchange code for GitHub token. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<GitHubTokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    private async Task<GitHubUserDetails?> GetGitHubUserDetails(string accessToken, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd(GitHubUserAgent);
        userRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get GitHub user info. Status: {StatusCode}", userResponse.StatusCode);
            return null;
        }

        var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<GitHubUserInfo>(userContent, options);

        if (userInfo == null)
        {
            _logger.LogError("Failed to deserialize GitHub user info");
            return null;
        }

        var email = userInfo.Email;
        var emailVerified = false;

        if (string.IsNullOrEmpty(email))
        {
            var emails = await GetGitHubEmails(accessToken, cancellationToken);
            var primaryEmail = emails?
                .OrderByDescending(e => e.Primary)
                .ThenByDescending(e => e.Verified)
                .FirstOrDefault();

            if (primaryEmail != null)
            {
                email = primaryEmail.Email;
                emailVerified = primaryEmail.Verified;
            }
        }
        else
        {
            emailVerified = true;
        }

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("GitHub user {UserId} does not have an accessible email address", userInfo.Id);
            return null;
        }

        return new GitHubUserDetails(
            Id: userInfo.Id.ToString(),
            Login: userInfo.Login,
            Name: userInfo.Name,
            Email: email,
            EmailVerified: emailVerified,
            AvatarUrl: userInfo.AvatarUrl
        );
    }

    private async Task<GitHubEmailInfo[]?> GetGitHubEmails(string accessToken, CancellationToken cancellationToken)
    {
        var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        emailRequest.Headers.UserAgent.ParseAdd(GitHubUserAgent);
        emailRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var emailResponse = await _httpClient.SendAsync(emailRequest, cancellationToken);

        if (!emailResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to load GitHub account emails. Status: {StatusCode}", emailResponse.StatusCode);
            return null;
        }

        var content = await emailResponse.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<GitHubEmailInfo[]>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    private async Task<User> FindOrCreateGitHubUser(GitHubUserDetails githubUser, CancellationToken cancellationToken)
    {
        var existingExternalLogin = await _context.ExternalLogins
            .Include(el => el.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(el => el.User)
                .ThenInclude(u => u.UserTiers)
                    .ThenInclude(ut => ut.Tier)
            .FirstOrDefaultAsync(el => el.Provider == "github" && el.ProviderUserId == githubUser.Id, cancellationToken);

        if (existingExternalLogin != null)
        {
            _logger.LogInformation("Found existing GitHub user {UserId}", existingExternalLogin.User.Id);
            var saveRequired = false;

            if (!existingExternalLogin.User.EmailVerified && githubUser.EmailVerified)
            {
                existingExternalLogin.User.EmailVerified = true;
                existingExternalLogin.User.UpdatedAt = DateTimeOffset.UtcNow;
                saveRequired = true;
            }

            if (await TryUpdateAvatarFromProviderAsync(existingExternalLogin.User, githubUser.AvatarUrl, "github", cancellationToken))
            {
                saveRequired = true;
            }

            if (saveRequired)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return existingExternalLogin.User;
        }

        var existingUser = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTiers)
                .ThenInclude(ut => ut.Tier)
            .FirstOrDefaultAsync(u => u.Email == githubUser.Email, cancellationToken);

        if (existingUser != null)
        {
            var externalLogin = new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = existingUser.Id,
                Provider = "github",
                ProviderUserId = githubUser.Id,
                ProviderEmail = githubUser.Email,
                LinkedAt = DateTimeOffset.UtcNow
            };

            _context.ExternalLogins.Add(externalLogin);

            if (!existingUser.EmailVerified && githubUser.EmailVerified)
            {
                existingUser.EmailVerified = true;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await TryUpdateAvatarFromProviderAsync(existingUser, githubUser.AvatarUrl, "github", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked GitHub account to existing user {UserId}", existingUser.Id);
            return existingUser;
        }

        var now = DateTimeOffset.UtcNow;

        var freeTier = await _context.Tiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == (int)TierType.Free, cancellationToken);

        if (freeTier is null)
        {
            _logger.LogError("Default free tier definition is missing. Unable to create GitHub OAuth user for {Email}", githubUser.Email);
            throw new InvalidOperationException("Default tier configuration is missing.");
        }

        var firstName = string.Empty;
        var lastName = string.Empty;
        var fullName = (githubUser.Name ?? string.Empty).Trim();

        if (!string.IsNullOrEmpty(fullName))
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                firstName = parts[0];
            }
            else if (parts.Length >= 2)
            {
                firstName = parts[0];
                lastName = parts[^1];
            }
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            firstName = githubUser.Login;
        }

        var usernameSeed = !string.IsNullOrWhiteSpace(githubUser.Login)
            ? githubUser.Login
            : githubUser.Email.Split('@')[0];

        var displayName = !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : githubUser.Login;

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = githubUser.Email,
            EmailVerified = githubUser.EmailVerified,
            PasswordHash = GenerateRandomHash(),
            FirstName = firstName,
            LastName = lastName,
            Username = await GenerateUniqueUsername(usernameSeed, cancellationToken),
            DisplayName = displayName,
            AvatarUrl = githubUser.AvatarUrl,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var cachedAvatar = await CacheExternalAvatarAsync(newUser.Id, "github", githubUser.AvatarUrl, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedAvatar))
        {
            newUser.AvatarUrl = cachedAvatar;
        }

        var newUserTier = new UserTier
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            TierId = freeTier.Id,
            ActiveFrom = now,
            ActiveUntil = null,
            IsActive = true,
            Notes = "Assigned during GitHub OAuth registration",
            CreatedAt = now,
            UpdatedAt = now
        };

        newUser.UserTiers.Add(newUserTier);

        _context.Users.Add(newUser);
        _context.UserTiers.Add(newUserTier);

        var newExternalLogin = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Provider = "github",
            ProviderUserId = githubUser.Id,
            ProviderEmail = githubUser.Email,
            LinkedAt = now
        };

        _context.ExternalLogins.Add(newExternalLogin);

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user", cancellationToken);
        if (defaultRole != null)
        {
            var userRoleLink = new UserRole
            {
                UserId = newUser.Id,
                RoleId = defaultRole.Id
            };

            _context.UserRoles.Add(userRoleLink);
            newUser.UserRoles.Add(userRoleLink);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user from GitHub OAuth {UserId}", newUser.Id);

        try
        {
            await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.FirstName ?? newUser.Username);
            _logger.LogInformation("Welcome email sent to new GitHub OAuth user {Email}", newUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to new GitHub OAuth user {Email}", newUser.Email);
        }

        return newUser;
    }

    private sealed record GitHubUserDetails(
        string Id,
        string Login,
        string? Name,
        string Email,
        bool EmailVerified,
        string? AvatarUrl
    );

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
            .Include(el => el.User)
                .ThenInclude(u => u.UserTiers)
                    .ThenInclude(ut => ut.Tier)
            .FirstOrDefaultAsync(el => el.Provider == "google" && el.ProviderUserId == googleUser.Id, cancellationToken);

        if (existingExternalLogin != null)
        {
            _logger.LogInformation("Found existing Google user {UserId}", existingExternalLogin.User.Id);
            var saveRequired = false;

            // Ensure user's email is marked verified when they sign in via Google
            if (!existingExternalLogin.User.EmailVerified)
            {
                existingExternalLogin.User.EmailVerified = true;
                existingExternalLogin.User.UpdatedAt = DateTimeOffset.UtcNow;
                saveRequired = true;
            }

            if (await TryUpdateAvatarFromProviderAsync(existingExternalLogin.User, googleUser.Picture, "google", cancellationToken))
            {
                saveRequired = true;
            }

            if (saveRequired)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return existingExternalLogin.User;
        }

        // Check if user exists by email
        var existingUser = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTiers)
                .ThenInclude(ut => ut.Tier)
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
            // If the existing user's email wasn't verified, trust Google's verification and mark it verified
            if (!existingUser.EmailVerified)
            {
                existingUser.EmailVerified = true;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await TryUpdateAvatarFromProviderAsync(existingUser, googleUser.Picture, "google", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked Google account to existing user {UserId}", existingUser.Id);
            return existingUser;
        }

        // Parse display name into first and last name (if possible)
        string parsedFirstName = googleUser.GivenName;
        string parsedLastName = googleUser.FamilyName;
        var fullName = (googleUser.Name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(parsedFirstName) && !string.IsNullOrEmpty(fullName))
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                parsedFirstName = parts[0];
                parsedLastName = string.Empty;
            }
            else if (parts.Length >= 2)
            {
                parsedFirstName = parts[0];
                parsedLastName = parts[^1]; // last word as last name
            }
        }

        // Create new user
        var now = DateTimeOffset.UtcNow;

        var freeTier = await _context.Tiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == (int)TierType.Free, cancellationToken);

        if (freeTier is null)
        {
            _logger.LogError("Default free tier definition is missing. Unable to create Google OAuth user for {Email}", googleUser.Email);
            throw new InvalidOperationException("Default tier configuration is missing.");
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = googleUser.Email,
            // Always treat Google signups as verified (Google verifies ownership)
            EmailVerified = true,
            PasswordHash = GenerateRandomHash(), // They won't use password login
            FirstName = parsedFirstName,
            LastName = parsedLastName,
            Username = await GenerateUniqueUsername(fullName != string.Empty ? fullName : googleUser.Email.Split('@')[0], cancellationToken),
            DisplayName = string.IsNullOrEmpty(parsedFirstName) ? fullName : parsedFirstName,
            AvatarUrl = googleUser.Picture,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var cachedAvatar = await CacheExternalAvatarAsync(newUser.Id, "google", googleUser.Picture, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedAvatar))
        {
            newUser.AvatarUrl = cachedAvatar;
        }

        var newUserTier = new UserTier
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            TierId = freeTier.Id,
            ActiveFrom = now,
            ActiveUntil = null,
            IsActive = true,
            Notes = "Assigned during Google OAuth registration",
            CreatedAt = now,
            UpdatedAt = now
        };

        newUser.UserTiers.Add(newUserTier);

        _context.Users.Add(newUser);
        _context.UserTiers.Add(newUserTier);

        // Add external login record
        var newExternalLogin = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Provider = "google",
            ProviderUserId = googleUser.Id,
            ProviderEmail = googleUser.Email,
            LinkedAt = now
        };

        _context.ExternalLogins.Add(newExternalLogin);

        // Assign default user role (match the registration pattern)
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user", cancellationToken);
        if (defaultRole != null)
        {
            var userRoleLink = new UserRole
            {
                UserId = newUser.Id,
                RoleId = defaultRole.Id
            };

            _context.UserRoles.Add(userRoleLink);
            newUser.UserRoles.Add(userRoleLink);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user from Google OAuth {UserId}", newUser.Id);

        // Send welcome email for new Google OAuth users
        try
        {
            await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.FirstName ?? "User");
            _logger.LogInformation("Welcome email sent to new Google OAuth user {Email}", newUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to new Google OAuth user {Email}", newUser.Email);
            // Continue without failing - user creation was successful
        }

        return newUser;
    }

    private async Task<bool> TryUpdateAvatarFromProviderAsync(
        User user,
        string? externalUrl,
        string provider,
        CancellationToken cancellationToken)
    {
        if (!ShouldReplaceAvatar(user.AvatarUrl, provider) || string.IsNullOrWhiteSpace(externalUrl))
        {
            return false;
        }

        var cachedUrl = await CacheExternalAvatarAsync(user.Id, provider, externalUrl, cancellationToken);
        if (string.IsNullOrWhiteSpace(cachedUrl) ||
            string.Equals(user.AvatarUrl, cachedUrl, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        user.AvatarUrl = cachedUrl;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    private async Task<string?> CacheExternalAvatarAsync(
        Guid userId,
        string provider,
        string? externalUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalUrl))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, externalUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            request.Headers.UserAgent.ParseAdd(ExternalAvatarUserAgent);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to download {Provider} avatar for user {UserId}. StatusCode {StatusCode}",
                    provider,
                    userId,
                    response.StatusCode);
                return null;
            }

            var headerContentType = response.Content.Headers.ContentType?.MediaType;
            var resolvedContentType = ResolveContentType(headerContentType, externalUrl);

            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var uploadResult = await _blobStorageService.UploadImageAsync(
                sourceStream,
                $"{provider}-avatar{GetExtensionFromContentType(resolvedContentType)}",
                resolvedContentType,
                $"{userId:N}-avatar",
                cancellationToken);

            return uploadResult.Url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to cache {Provider} avatar for user {UserId}",
                provider,
                userId);
            return null;
        }
    }

    private static bool ShouldReplaceAvatar(string? currentAvatarUrl, string provider)
    {
        if (string.IsNullOrWhiteSpace(currentAvatarUrl))
        {
            return true;
        }

        return provider.ToLowerInvariant() switch
        {
            "google" => currentAvatarUrl.Contains("googleusercontent.com", StringComparison.OrdinalIgnoreCase),
            "github" => currentAvatarUrl.Contains("githubusercontent.com", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string ResolveContentType(string? headerContentType, string sourceUrl)
    {
        if (!string.IsNullOrWhiteSpace(headerContentType))
        {
            return headerContentType;
        }

        var extension = ExtractExtension(sourceUrl);
        return extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            ".svg" => "image/svg+xml",
            _ => "image/jpeg"
        };
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/tiff" => ".tiff",
            "image/svg+xml" => ".svg",
            _ => ".jpg"
        };
    }

    private static string ExtractExtension(string sourceUrl)
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext.ToLowerInvariant();
            }
        }

        return string.Empty;
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

        // Generate JWT with session ID
        var accessToken = _jwtService.GenerateToken(user, session.Id);

        var userInfo = await BuildUserInfoAsync(
            user,
            authMethod: authMethod,
            cancellationToken: cancellationToken);

        return new AuthResponse(accessToken, refreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15), userInfo);
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

    private static bool HasUsablePassword(string? passwordHash)
        => !string.IsNullOrEmpty(passwordHash) &&
           passwordHash.StartsWith("$2", StringComparison.Ordinal);

    // Helper to build a UserInfo response with safe fallbacks for username and names
    private async Task<UserInfo> BuildUserInfoAsync(
        User user,
        IEnumerable<string>? rolesOverride = null,
        string? authMethod = null,
        CancellationToken cancellationToken = default,
        UserTierInfo? activeTierOverride = null)
    {
        // Ensure username - if missing, generate a generic one: user + short id
        var username = string.IsNullOrWhiteSpace(user.Username)
            ? $"user{user.Id.ToString().Split('-')[0]}"
            : user.Username;

        // Default names to empty strings to avoid null in frontends
        var firstName = user.FirstName ?? string.Empty;
        var lastName = user.LastName ?? string.Empty;

        var roles = rolesOverride != null
            ? rolesOverride.ToArray()
            : user.UserRoles?.Select(ur => ur.Role.Name).ToArray() ?? Array.Empty<string>();

        var activeTier = activeTierOverride ?? ResolveActiveTier(user);
        activeTier ??= await GetActiveTierInfoAsync(user.Id, cancellationToken);

        return new UserInfo(
            Id: user.Id,
            Email: user.Email,
            EmailVerified: user.EmailVerified,
            Username: username,
            FirstName: firstName,
            LastName: lastName,
            AvatarUrl: user.AvatarUrl,
            CoverUrl: user.CoverUrl,
            CreatedAt: user.CreatedAt,
            Roles: roles,
            AuthMethod: authMethod,
            ActiveTier: activeTier
        );
    }

    private static UserTierInfo? ResolveActiveTier(User user)
    {
        if (user.UserTiers is null || user.UserTiers.Count == 0)
            return null;

        var now = DateTimeOffset.UtcNow;

        var active = user.UserTiers
            .Where(ut => ut.IsActive &&
                         ut.ActiveFrom <= now &&
                         (ut.ActiveUntil == null || ut.ActiveUntil >= now))
            .OrderByDescending(ut => ut.ActiveFrom)
            .FirstOrDefault();

        if (active?.Tier is null)
            return null;

        return new UserTierInfo(
            active.TierId,
            active.Tier.Name,
            active.ActiveFrom,
            active.ActiveUntil
        );
    }

    private async Task<UserTierInfo?> GetActiveTierInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.UserTiers
            .AsNoTracking()
            .Where(ut => ut.UserId == userId &&
                         ut.IsActive &&
                         ut.ActiveFrom <= now &&
                         (ut.ActiveUntil == null || ut.ActiveUntil >= now))
            .OrderByDescending(ut => ut.ActiveFrom)
            .Select(ut => new UserTierInfo(
                ut.TierId,
                ut.Tier.Name,
                ut.ActiveFrom,
                ut.ActiveUntil
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Helper method for consistent cookie options
    private CookieOptions CreateSecureCookieOptions(TimeSpan? maxAge = null)
    {
        var isHttps = HttpContext.Request.IsHttps;
        var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", false) ||
                           !_configuration.GetValue<bool>("IsProduction", false);

        _logger.LogInformation("Cookie configuration - HTTPS: {IsHttps}, Development: {IsDevelopment}", isHttps, isDevelopment);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps, // Only secure on HTTPS, allow HTTP for localhost development
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax, // Lax for HTTP localhost development
            Path = "/", // Make cookie available site-wide
            MaxAge = maxAge
        };

        // In development, set domain to localhost so it works across different ports
        if (isDevelopment && HttpContext.Request.Host.Host == "localhost")
        {
            cookieOptions.Domain = "localhost";
            _logger.LogInformation("Setting cookie domain to 'localhost' for development cross-port access");
        }

        return cookieOptions;
    }

}
