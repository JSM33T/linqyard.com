using Linqyard.Worker.Services;

namespace Linqyard.Worker.Workers;

public class TierDowngradeWorker : BackgroundService
{
    private readonly ILogger<TierDowngradeWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _runInterval;

    public TierDowngradeWorker(
        ILogger<TierDowngradeWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var intervalConfig = configuration.GetSection("TierDowngrade:RunInterval").Value;
        _runInterval = string.IsNullOrEmpty(intervalConfig)
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.Parse(intervalConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tier Downgrade Worker started. Will run every {Interval}", _runInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting tier downgrade cycle at {Time}", DateTimeOffset.Now);

                using var scope = _serviceProvider.CreateScope();
                var tierDowngradeService = scope.ServiceProvider.GetRequiredService<TierDowngradeService>();
                await tierDowngradeService.ProcessExpiredTiersAsync(stoppingToken);

                _logger.LogInformation("Tier downgrade cycle completed at {Time}. Next run in {Interval}",
                    DateTimeOffset.Now, _runInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during tier downgrade cycle");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Tier Downgrade Worker stopped");
    }
}
