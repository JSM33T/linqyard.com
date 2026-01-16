using Linqyard.Worker.Services;

namespace Linqyard.Worker.Workers;

public class AccountCleanupWorker : BackgroundService
{
    private readonly ILogger<AccountCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _runInterval;

    public AccountCleanupWorker(
        ILogger<AccountCleanupWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var intervalConfig = configuration.GetSection("AccountCleanup:RunInterval").Value;
        _runInterval = string.IsNullOrEmpty(intervalConfig)
            ? TimeSpan.FromMinutes(10)
            : TimeSpan.Parse(intervalConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Account Cleanup Worker started. Will run every {Interval}", _runInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting account cleanup cycle at {Time}", DateTimeOffset.Now);

                using var scope = _serviceProvider.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<AccountCleanupService>();
                await cleanupService.CleanupUnverifiedUsersAsync(stoppingToken);

                _logger.LogInformation("Account cleanup cycle completed at {Time}. Next run in {Interval}",
                    DateTimeOffset.Now, _runInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account cleanup cycle");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Account Cleanup Worker stopped");
    }
}
