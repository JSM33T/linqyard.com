using Linqyard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Linqyard.Api.Services;

/// <summary>
/// Background service that periodically cleans up unverified accounts
/// that have exceeded the verification grace period
/// </summary>
public class UnverifiedAccountCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnverifiedAccountCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval;

    public UnverifiedAccountCleanupService(
        IServiceProvider serviceProvider,
        ILogger<UnverifiedAccountCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Run cleanup check every 24 hours by default
        var intervalHours = _configuration.GetValue<int>("Auth:UnverifiedAccountCleanupIntervalHours", 24);
        _checkInterval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unverified Account Cleanup Service started. Check interval: {Interval}", _checkInterval);

        // Wait a bit before first run to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupUnverifiedAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during unverified account cleanup");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupUnverifiedAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LinqyardDbContext>();

        // Get grace period from configuration (default 30 days)
        var gracePeriodDays = _configuration.GetValue<int>("Auth:UnverifiedAccountGracePeriodDays", 30);
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-gracePeriodDays);

        _logger.LogInformation("Starting cleanup of unverified accounts older than {CutoffDate} ({Days} days)", 
            cutoffDate, gracePeriodDays);

        // Find unverified accounts older than the grace period
        var staleAccounts = await context.Users
            .Where(u => !u.EmailVerified && u.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (staleAccounts.Count == 0)
        {
            _logger.LogInformation("No stale unverified accounts found");
            return;
        }

        _logger.LogInformation("Found {Count} stale unverified accounts to remove", staleAccounts.Count);

        // Delete the accounts (cascade delete will handle related records)
        context.Users.RemoveRange(staleAccounts);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully removed {Count} stale unverified accounts", staleAccounts.Count);
    }
}
