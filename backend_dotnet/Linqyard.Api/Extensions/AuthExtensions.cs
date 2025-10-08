using Linqyard.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;

namespace Linqyard.Api.Extensions;

/// <summary>
/// Provides extension methods for configuring JWT authentication and authorization.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Configures JWT authentication and authorization using the provided <see cref="JwtSettings"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration containing the "JWT" section.</param>
    /// <param name="env">The current host environment.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the JWT configuration section is missing or invalid (e.g., missing secret key).
    /// </exception>
    /// <example>
    /// Example configuration in <c>appsettings.json</c>:
    /// <code json>
    /// {
    ///   "JWT": {
    ///     "Issuer": "your-issuer",
    ///     "Audience": "your-audience",
    ///     "SecretKey": "your-very-strong-secret"
    ///   }
    /// }
    /// </code>
    ///
    /// Example usage in <c>Program.cs</c>:
    /// <code>
    /// builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
    /// </code>
    /// </example>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JWT"));
        var jwtSettings = configuration.GetSection("JWT").Get<JwtSettings>();

        if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT configuration is missing or invalid");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !env.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    Log.Information("JWT token validated for user {UserId}", userId);
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
