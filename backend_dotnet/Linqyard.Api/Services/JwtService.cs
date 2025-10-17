using System;
using System.Linq;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Linqyard.Api.Configuration;
using Linqyard.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Linqyard.Api.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateToken(User user, Guid? sessionId = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("user_id", user.Id.ToString()),
            new("username", user.Username),
            new("email_verified", user.EmailVerified.ToString().ToLower())
        };

        // Add session ID if provided
        if (sessionId.HasValue)
        {
            claims.Add(new Claim("session_id", sessionId.Value.ToString()));
            claims.Add(new Claim("sid", sessionId.Value.ToString())); // Standard session ID claim
        }

        // Add display name if available
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim("display_name", user.DisplayName));
        }

        if (user.UserRoles is not null && user.UserRoles.Count > 0)
        {
            var roleNames = user.UserRoles
                .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
                .Select(ur => ur.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("Generated JWT token for user {UserId} ({Username}) - IssuedAt: {IssuedAt}, NotBefore: {NotBefore}, Expires: {Expires}", 
            user.Id, user.Username, tokenDescriptor.IssuedAt, tokenDescriptor.NotBefore, tokenDescriptor.Expires);
        
        return tokenString;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew for token validation
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            if (validatedToken is JwtSecurityToken jwtToken && 
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return principal;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return null;
        }
    }

    public int GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Invalid or expired token");
    }
}
