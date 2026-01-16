using Linqyard.Worker.Services;

namespace Linqyard.Worker.Workers;

public class GeolocationEnrichmentWorker : BackgroundService
{
    private readonly ILogger<GeolocationEnrichmentWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _runInterval;

    public GeolocationEnrichmentWorker(
        ILogger<GeolocationEnrichmentWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var intervalConfig = configuration.GetSection("GeolocationEnrichment:RunInterval").Value;
        _runInterval = string.IsNullOrEmpty(intervalConfig)
            ? TimeSpan.FromHours(1)
            : TimeSpan.Parse(intervalConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Geolocation Enrichment Worker started. Will run every {Interval}", _runInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting geolocation enrichment cycle at {Time}", DateTimeOffset.Now);

                using var scope = _serviceProvider.CreateScope();
                var enrichmentService = scope.ServiceProvider.GetRequiredService<GeolocationEnrichmentService>();
                await enrichmentService.EnrichGeolocationDataAsync(stoppingToken);

                _logger.LogInformation("Geolocation enrichment cycle completed at {Time}. Next run in {Interval}",
                    DateTimeOffset.Now, _runInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during geolocation enrichment cycle");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Geolocation Enrichment Worker stopped");
    }
}
