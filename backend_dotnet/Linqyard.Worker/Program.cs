using Linqyard.Worker;
using Linqyard.Worker.Configuration;
using Linqyard.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Linqyard.Data;
using Linqyard.Repositories;
using Linqyard.Contracts.Interfaces;
using Polly;
using Polly.Extensions.Http;

var builder = Host.CreateApplicationBuilder(args);

// Configure options
builder.Services.Configure<IpGeolocationOptions>(
    builder.Configuration.GetSection("IpGeolocation"));
builder.Services.Configure<GeolocationEnrichmentOptions>(
    builder.Configuration.GetSection("GeolocationEnrichment"));

// Register services
builder.Services.AddMemoryCache();

// Register a named HttpClient for IpGeolocationService with Polly retry/backoff
// Register the retry policy and a delegating handler adapter to apply it
builder.Services.AddSingleton<IAsyncPolicy<System.Net.Http.HttpResponseMessage>>(_ => GetRetryPolicy());
builder.Services.AddTransient<PollyHttpMessageHandlerAdapter>();
builder.Services.AddHttpClient(nameof(IpGeolocationService))
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddHttpMessageHandler<PollyHttpMessageHandlerAdapter>();

builder.Services.AddSingleton<IIpGeolocationService, IpGeolocationService>();
builder.Services.AddScoped<GeolocationEnrichmentService>();

// Database + repositories required by AccountCleanupService
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<LinqyardDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<AccountCleanupService>();
}

// Register hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // Retry on transient errors and 429 with exponential backoff
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => (int)msg.StatusCode == 429)
        .WaitAndRetryAsync(new[] {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        }, onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            // No-op here; IpGeolocationService will log if needed
        });
}
