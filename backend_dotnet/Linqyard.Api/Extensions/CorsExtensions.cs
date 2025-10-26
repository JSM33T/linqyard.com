using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Linqyard.Api.Extensions
{
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
        /// 
        /// Allowed hosts:
        /// - linqyard.com and any subdomain (*.linqyard.com)
        /// - localhost and 127.0.0.1 (any port)
        /// - any Azure Dev Tunnel host (*.devtunnels.ms)
        /// 
        /// Notes:
        /// - Uses SetIsOriginAllowed to support wildcard-like checks for devtunnels.ms.
        /// - AllowCredentials is enabled for cookie/authorization header scenarios.
        /// </summary>
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

                                // Production: root + subdomains of linqyard.com
                                if (host.Equals("linqyard.com", StringComparison.OrdinalIgnoreCase)) return true;
                                if (host.EndsWith(".linqyard.com", StringComparison.OrdinalIgnoreCase)) return true;

                                // Local development
                                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
                                if (host.Equals("127.0.0.1")) return true;

                                // Azure Dev Tunnels (e.g., 97jk4d90-3000.inc1.devtunnels.ms)
                                if (host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase)) return true;

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
        public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
        {
            app.UseCors(AllowFrontendPolicy);
            return app;
        }
    }
}
