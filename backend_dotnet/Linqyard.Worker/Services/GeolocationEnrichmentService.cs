using System.Net;
using Linqyard.Worker.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Linqyard.Worker.Services;

public sealed class GeolocationEnrichmentService
{
    private readonly IConfiguration _configuration;
    private readonly IIpGeolocationService _geolocationService;
    private readonly GeolocationEnrichmentOptions _options;
    private readonly ILogger<GeolocationEnrichmentService> _logger;

    public GeolocationEnrichmentService(
        IConfiguration configuration,
        IIpGeolocationService geolocationService,
        IOptions<GeolocationEnrichmentOptions> options,
        ILogger<GeolocationEnrichmentService> logger)
    {
        _configuration = configuration;
        _geolocationService = geolocationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnrichGeolocationDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting geolocation enrichment job");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Database connection string not found");
            return;
        }

        int analyticsProcessed = 0;
        int viewTelemetriesProcessed = 0;

        try
        {
            if (_options.ProcessAnalytics)
            {
                analyticsProcessed = await EnrichAnalyticsAsync(connectionString, cancellationToken);
            }

            if (_options.ProcessViewTelemetries)
            {
                viewTelemetriesProcessed = await EnrichViewTelemetriesAsync(connectionString, cancellationToken);
            }

            _logger.LogInformation(
                "Geolocation enrichment completed. Analytics: {AnalyticsCount}, ViewTelemetries: {ViewTelemetriesCount}",
                analyticsProcessed,
                viewTelemetriesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during geolocation enrichment");
            throw;
        }
    }

    private async Task<int> EnrichAnalyticsAsync(string connectionString, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enriching Analytics table");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Find records with IpAddress but missing Country or City
        const string selectQuery = @"
            SELECT ""Id"", ""IpAddress""
            FROM public.""Analytics""
            WHERE ""IpAddress"" IS NOT NULL
              AND (""Country"" IS NULL OR ""City"" IS NULL)
            LIMIT @batchSize;";

        var recordsToUpdate = new List<(Guid Id, IPAddress IpAddress)>();

        await using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
        {
            selectCommand.Parameters.AddWithValue("batchSize", _options.BatchSize);

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var ipAddress = reader.GetFieldValue<IPAddress>(1);
                recordsToUpdate.Add((id, ipAddress));
            }
        }

        _logger.LogInformation("Found {Count} Analytics records to enrich (missing Country and/or City)", recordsToUpdate.Count);

        int processed = 0;
        foreach (var (id, ipAddress) in recordsToUpdate)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var geoData = await _geolocationService.ResolveAsync(ipAddress, cancellationToken);
                if (geoData is not null)
                {
                    await UpdateAnalyticsRecordAsync(connection, id, geoData, cancellationToken);
                    processed++;
                    _logger.LogDebug("Enriched Analytics record {Id} with geolocation data", id);
                }
                else
                {
                    _logger.LogDebug("No geolocation data available for IP {IpAddress} in Analytics {Id}", ipAddress, id);
                }

                // Delay to avoid rate limiting
                await Task.Delay(_options.DelayBetweenRecords, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich Analytics record {Id}", id);
            }
        }

        return processed;
    }

    private async Task<int> EnrichViewTelemetriesAsync(string connectionString, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enriching ViewTelemetries table");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Find records with IpAddress but missing Country or City
        const string selectQuery = @"
            SELECT ""Id"", ""IpAddress""
            FROM public.""ViewTelemetries""
            WHERE ""IpAddress"" IS NOT NULL
              AND (""Country"" IS NULL OR ""City"" IS NULL)
            LIMIT @batchSize;";

        var recordsToUpdate = new List<(Guid Id, IPAddress IpAddress)>();

        await using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
        {
            selectCommand.Parameters.AddWithValue("batchSize", _options.BatchSize);

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var ipAddress = reader.GetFieldValue<IPAddress>(1);
                recordsToUpdate.Add((id, ipAddress));
            }
        }

        _logger.LogInformation("Found {Count} ViewTelemetries records to enrich (missing Country and/or City)", recordsToUpdate.Count);

        int processed = 0;
        foreach (var (id, ipAddress) in recordsToUpdate)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var geoData = await _geolocationService.ResolveAsync(ipAddress, cancellationToken);
                if (geoData is not null)
                {
                    await UpdateViewTelemetryRecordAsync(connection, id, geoData, cancellationToken);
                    processed++;
                    _logger.LogDebug("Enriched ViewTelemetry record {Id} with geolocation data", id);
                }
                else
                {
                    _logger.LogDebug("No geolocation data available for IP {IpAddress} in ViewTelemetry {Id}", ipAddress, id);
                }

                // Delay to avoid rate limiting
                await Task.Delay(_options.DelayBetweenRecords, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich ViewTelemetry record {Id}", id);
            }
        }

        return processed;
    }

    private async Task UpdateAnalyticsRecordAsync(
        NpgsqlConnection connection,
        Guid id,
        IpGeolocationResult geoData,
        CancellationToken cancellationToken)
    {
        const string updateQuery = @"
            UPDATE public.""Analytics""
            SET ""Country"" = COALESCE(""Country"", @country),
                ""City"" = COALESCE(""City"", @city),
                ""Latitude"" = COALESCE(""Latitude"", @latitude),
                ""Longitude"" = COALESCE(""Longitude"", @longitude),
                ""Accuracy"" = COALESCE(""Accuracy"", @accuracy)
            WHERE ""Id"" = @id;";

        await using var command = new NpgsqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("country", NpgsqlDbType.Text, (object?)geoData.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("city", NpgsqlDbType.Text, (object?)geoData.City ?? DBNull.Value);
        command.Parameters.AddWithValue("latitude", NpgsqlDbType.Double, (object?)geoData.Latitude ?? DBNull.Value);
        command.Parameters.AddWithValue("longitude", NpgsqlDbType.Double, (object?)geoData.Longitude ?? DBNull.Value);
        command.Parameters.AddWithValue("accuracy", NpgsqlDbType.Double, (object?)geoData.AccuracyMeters ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateViewTelemetryRecordAsync(
        NpgsqlConnection connection,
        Guid id,
        IpGeolocationResult geoData,
        CancellationToken cancellationToken)
    {
        const string updateQuery = @"
            UPDATE public.""ViewTelemetries""
            SET ""Country"" = COALESCE(""Country"", @country),
                ""City"" = COALESCE(""City"", @city),
                ""Latitude"" = COALESCE(""Latitude"", @latitude),
                ""Longitude"" = COALESCE(""Longitude"", @longitude),
                ""Accuracy"" = COALESCE(""Accuracy"", @accuracy)
            WHERE ""Id"" = @id;";

        await using var command = new NpgsqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("country", NpgsqlDbType.Text, (object?)geoData.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("city", NpgsqlDbType.Text, (object?)geoData.City ?? DBNull.Value);
        command.Parameters.AddWithValue("latitude", NpgsqlDbType.Double, (object?)geoData.Latitude ?? DBNull.Value);
        command.Parameters.AddWithValue("longitude", NpgsqlDbType.Double, (object?)geoData.Longitude ?? DBNull.Value);
        command.Parameters.AddWithValue("accuracy", NpgsqlDbType.Double, (object?)geoData.AccuracyMeters ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
