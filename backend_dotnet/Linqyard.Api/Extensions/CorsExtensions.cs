using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Linqyard.Api.Extensions;

/// <summary>
/// Provides extension methods for configuring and applying custom CORS policies.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// The name of the CORS policy that allows requests from the frontend.
    /// </summary>
    public const string AllowFrontendPolicy = "AllowFrontend";

    /// <summary>
    /// Adds a custom CORS policy named <see cref="AllowFrontendPolicy"/> that allows
    /// requests from trusted frontend origins.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the policy to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// The configured policy allows requests from:
    /// <list type="bullet">
    ///   <item><description><c>http://localhost:3000</c></description></item>
    ///   <item><description><c>https://localhost:3000</c></description></item>
    ///   <item><description><c>https://linqyard.com</c></description></item>
    /// </list>
    /// 
    /// It also allows:
    /// <list type="bullet">
    ///   <item><description>Any HTTP method</description></item>
    ///   <item><description>Any request header</description></item>
    ///   <item><description>Credentials (cookies, authorization headers, etc.)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// In <c>Program.cs</c>:
    /// <code>
    /// builder.Services.AddCustomCors();
    /// </code>
    /// </example>
    public static IServiceCollection AddCustomCors_OLD(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllowFrontendPolicy, policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://linqyard.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllowFrontendPolicy, policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin)) return false;
                        try
                        {
                            var host = new Uri(origin).Host;
                            // allow root domain and any subdomain of linqyard.com
                            if (host.Equals("linqyard.com", StringComparison.OrdinalIgnoreCase)) return true;
                            if (host.EndsWith(".linqyard.com", StringComparison.OrdinalIgnoreCase)) return true;
                            // allow localhost for dev (any port)
                            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }


    /// <summary>
    /// Applies the <see cref="AllowFrontendPolicy"/> CORS policy to the application pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <example>
    /// In <c>Program.cs</c>:
    /// <code>
    /// var app = builder.Build();
    /// app.UseCustomCors();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
    {
        app.UseCors(AllowFrontendPolicy);
        return app;
    }
}
