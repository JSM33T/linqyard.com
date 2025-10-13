using System.Text.Json;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Linkyard.Repositories;

public class ViewTelemetryRepository : IViewTelemetryRepository
{
    private const string DefaultConnectionName = "DefaultConnection";

    private readonly ILogger<ViewTelemetryRepository> _logger;
    private readonly IConfiguration _configuration;

    public ViewTelemetryRepository(ILogger<ViewTelemetryRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task RecordProfileViewAsync(RecordProfileViewRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recording profile view for user {ProfileUserId}", request.ProfileUserId);

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        // Serialize UTM parameters to JSON if present
        string? utmJson = null;
        if (request.UtmParameters != null)
        {
            utmJson = JsonSerializer.Serialize(new
            {
                source = request.UtmParameters.Source,
                medium = request.UtmParameters.Medium,
                campaign = request.UtmParameters.Campaign,
                term = request.UtmParameters.Term,
                content = request.UtmParameters.Content
            });
        }

        const string commandText = @"
            INSERT INTO public.""ViewTelemetries"" (
                ""Id"", ""ProfileUserId"", ""ViewerUserId"", ""Fingerprint"", ""Source"", ""Referrer"",
                ""UtmParameters"", ""Latitude"", ""Longitude"", ""Accuracy"", ""City"", ""Country"",
                ""UserAgent"", ""DeviceType"", ""Os"", ""Browser"", ""IpAddress"", ""SessionId"", ""ViewedAt""
            ) VALUES (
                @id, @profileUserId, @viewerUserId, @fingerprint, @source, @referrer,
                @utmParameters, @latitude, @longitude, @accuracy, @city, @country,
                @userAgent, @deviceType, @os, @browser, @ipAddress, @sessionId, @viewedAt
            );";

        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = request.Id;
        command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = request.ProfileUserId;
        command.Parameters.Add("viewerUserId", NpgsqlDbType.Uuid).Value = (object?)request.ViewerUserId ?? DBNull.Value;
        command.Parameters.Add("fingerprint", NpgsqlDbType.Text).Value = (object?)request.Fingerprint ?? DBNull.Value;
        command.Parameters.Add("source", NpgsqlDbType.Text).Value = (object?)request.Source ?? DBNull.Value;
        command.Parameters.Add("referrer", NpgsqlDbType.Text).Value = (object?)request.Referrer ?? DBNull.Value;
        command.Parameters.Add("utmParameters", NpgsqlDbType.Jsonb).Value = (object?)utmJson ?? DBNull.Value;
        command.Parameters.Add("latitude", NpgsqlDbType.Double).Value = (object?)request.Latitude ?? DBNull.Value;
        command.Parameters.Add("longitude", NpgsqlDbType.Double).Value = (object?)request.Longitude ?? DBNull.Value;
        command.Parameters.Add("accuracy", NpgsqlDbType.Double).Value = (object?)request.Accuracy ?? DBNull.Value;
        command.Parameters.Add("city", NpgsqlDbType.Text).Value = (object?)request.City ?? DBNull.Value;
        command.Parameters.Add("country", NpgsqlDbType.Text).Value = (object?)request.Country ?? DBNull.Value;
        command.Parameters.Add("userAgent", NpgsqlDbType.Text).Value = (object?)request.UserAgent ?? DBNull.Value;
        command.Parameters.Add("deviceType", NpgsqlDbType.Text).Value = (object?)request.DeviceType ?? DBNull.Value;
        command.Parameters.Add("os", NpgsqlDbType.Text).Value = (object?)request.Os ?? DBNull.Value;
        command.Parameters.Add("browser", NpgsqlDbType.Text).Value = (object?)request.Browser ?? DBNull.Value;
        command.Parameters.Add("ipAddress", NpgsqlDbType.Inet).Value = (object?)request.IpAddress ?? DBNull.Value;
        command.Parameters.Add("sessionId", NpgsqlDbType.Text).Value = (object?)request.SessionId ?? DBNull.Value;
        command.Parameters.Add("viewedAt", NpgsqlDbType.TimestampTz).Value = request.ViewedAt;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ProfileViewsPageResponse> GetProfileViewsAsync(GetProfileViewsRequest request, CancellationToken cancellationToken = default)
    {
        var views = new List<ProfileViewResponse>();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        // Build WHERE clause dynamically
        var whereClauses = new List<string> { @"vt.""ProfileUserId"" = @profileUserId" };
        if (request.StartDate.HasValue)
            whereClauses.Add(@"vt.""ViewedAt"" >= @startDate");
        if (request.EndDate.HasValue)
            whereClauses.Add(@"vt.""ViewedAt"" <= @endDate");
        if (!string.IsNullOrEmpty(request.Source))
            whereClauses.Add(@"vt.""Source"" = @source");

        var whereClause = string.Join(" AND ", whereClauses);

        // Get total count
        var countQuery = $@"
            SELECT COUNT(*)::int
            FROM public.""ViewTelemetries"" AS vt
            WHERE {whereClause};";

        int total;
        await using (var countCommand = new NpgsqlCommand(countQuery, connection))
        {
            countCommand.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = request.ProfileUserId;
            if (request.StartDate.HasValue)
                countCommand.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = request.StartDate.Value;
            if (request.EndDate.HasValue)
                countCommand.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = request.EndDate.Value;
            if (!string.IsNullOrEmpty(request.Source))
                countCommand.Parameters.Add("source", NpgsqlDbType.Text).Value = request.Source;

            total = (int)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        // Get paginated data
        var dataQuery = $@"
            SELECT 
                vt.""Id"", vt.""ProfileUserId"", vt.""ViewerUserId"", u.""Username"" AS ""ViewerUsername"",
                vt.""Fingerprint"", vt.""Source"", vt.""Referrer"",
                vt.""UtmParameters"", vt.""Latitude"", vt.""Longitude"", vt.""Accuracy"",
                vt.""City"", vt.""Country"", vt.""UserAgent"", vt.""DeviceType"", vt.""Os"", vt.""Browser"",
                HOST(vt.""IpAddress"") AS ""IpAddress"", vt.""SessionId"", vt.""DurationSeconds"", vt.""ViewedAt""
            FROM public.""ViewTelemetries"" AS vt
            LEFT JOIN public.""Users"" AS u ON vt.""ViewerUserId"" = u.""Id""
            WHERE {whereClause}
            ORDER BY vt.""ViewedAt"" DESC
            OFFSET @skip LIMIT @take;";

        await using (var dataCommand = new NpgsqlCommand(dataQuery, connection))
        {
            dataCommand.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = request.ProfileUserId;
            if (request.StartDate.HasValue)
                dataCommand.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = request.StartDate.Value;
            if (request.EndDate.HasValue)
                dataCommand.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = request.EndDate.Value;
            if (!string.IsNullOrEmpty(request.Source))
                dataCommand.Parameters.Add("source", NpgsqlDbType.Text).Value = request.Source;
            dataCommand.Parameters.Add("skip", NpgsqlDbType.Integer).Value = request.Skip;
            dataCommand.Parameters.Add("take", NpgsqlDbType.Integer).Value = request.Take;

            await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var profileUserId = reader.GetGuid(1);
                Guid? viewerUserId = reader.IsDBNull(2) ? null : reader.GetGuid(2);
                var viewerUsername = reader.IsDBNull(3) ? null : reader.GetString(3);
                var fingerprint = reader.IsDBNull(4) ? null : reader.GetString(4);
                var source = reader.IsDBNull(5) ? null : reader.GetString(5);
                var referrer = reader.IsDBNull(6) ? null : reader.GetString(6);
                var utmJson = reader.IsDBNull(7) ? null : reader.GetString(7);
                double? latitude = reader.IsDBNull(8) ? null : reader.GetDouble(8);
                double? longitude = reader.IsDBNull(9) ? null : reader.GetDouble(9);
                double? accuracy = reader.IsDBNull(10) ? null : reader.GetDouble(10);
                var city = reader.IsDBNull(11) ? null : reader.GetString(11);
                var country = reader.IsDBNull(12) ? null : reader.GetString(12);
                var userAgent = reader.IsDBNull(13) ? null : reader.GetString(13);
                var deviceType = reader.IsDBNull(14) ? null : reader.GetString(14);
                var os = reader.IsDBNull(15) ? null : reader.GetString(15);
                var browser = reader.IsDBNull(16) ? null : reader.GetString(16);
                var ipAddress = reader.IsDBNull(17) ? null : reader.GetString(17);
                var sessionId = reader.IsDBNull(18) ? null : reader.GetString(18);
                int? durationSeconds = reader.IsDBNull(19) ? null : reader.GetInt32(19);
                var viewedAt = reader.GetFieldValue<DateTimeOffset>(20);

                // Parse UTM parameters
                string? utmSource = null, utmMedium = null, utmCampaign = null;
                if (!string.IsNullOrEmpty(utmJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(utmJson);
                        if (doc.RootElement.TryGetProperty("source", out var srcProp))
                            utmSource = srcProp.GetString();
                        if (doc.RootElement.TryGetProperty("medium", out var medProp))
                            utmMedium = medProp.GetString();
                        if (doc.RootElement.TryGetProperty("campaign", out var campProp))
                            utmCampaign = campProp.GetString();
                    }
                    catch { /* ignore JSON parse errors */ }
                }

                views.Add(new ProfileViewResponse(
                    id, profileUserId, viewerUserId, viewerUsername, fingerprint, source, referrer,
                    utmSource, utmMedium, utmCampaign, latitude, longitude, accuracy, city, country,
                    userAgent, deviceType, os, browser, ipAddress, sessionId, durationSeconds, viewedAt
                ));
            }
        }

        return new ProfileViewsPageResponse(views, total, request.Skip, request.Take);
    }

    public async Task<ProfileViewStatsResponse> GetProfileViewStatsAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string> { @"""ProfileUserId"" = @profileUserId" };
        if (startDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" >= @startDate");
        if (endDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" <= @endDate");
        var whereClause = string.Join(" AND ", whereClauses);

        // Get total views and unique visitors
        var statsQuery = $@"
            SELECT 
                COUNT(*)::bigint AS ""TotalViews"",
                COUNT(DISTINCT ""Fingerprint"")::bigint AS ""UniqueVisitors"",
                AVG(""DurationSeconds"") AS ""AvgDuration""
            FROM public.""ViewTelemetries""
            WHERE {whereClause};";

        long totalViews = 0, uniqueVisitors = 0;
        double avgDuration = 0;

        await using (var command = new NpgsqlCommand(statsQuery, connection))
        {
            command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
            if (startDate.HasValue)
                command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
            if (endDate.HasValue)
                command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                totalViews = reader.GetInt64(0);
                uniqueVisitors = reader.GetInt64(1);
                avgDuration = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
            }
        }

        // Get views by source
        var viewsBySource = new Dictionary<string, long>();
        var sourceQuery = $@"
            SELECT COALESCE(""Source"", 'unknown') AS ""Source"", COUNT(*)::bigint AS ""Count""
            FROM public.""ViewTelemetries""
            WHERE {whereClause}
            GROUP BY ""Source"";";

        await using (var command = new NpgsqlCommand(sourceQuery, connection))
        {
            command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
            if (startDate.HasValue)
                command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
            if (endDate.HasValue)
                command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var source = reader.GetString(0);
                var count = reader.GetInt64(1);
                viewsBySource[source] = count;
            }
        }

        // Get views by country
        var viewsByCountry = new Dictionary<string, long>();
        var countryQuery = $@"
            SELECT COALESCE(""Country"", 'unknown') AS ""Country"", COUNT(*)::bigint AS ""Count""
            FROM public.""ViewTelemetries""
            WHERE {whereClause}
            GROUP BY ""Country"";";

        await using (var command = new NpgsqlCommand(countryQuery, connection))
        {
            command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
            if (startDate.HasValue)
                command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
            if (endDate.HasValue)
                command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var country = reader.GetString(0);
                var count = reader.GetInt64(1);
                viewsByCountry[country] = count;
            }
        }

        // Get views by device
        var viewsByDevice = new Dictionary<string, long>();
        var deviceQuery = $@"
            SELECT COALESCE(""DeviceType"", 'unknown') AS ""DeviceType"", COUNT(*)::bigint AS ""Count""
            FROM public.""ViewTelemetries""
            WHERE {whereClause}
            GROUP BY ""DeviceType"";";

        await using (var command = new NpgsqlCommand(deviceQuery, connection))
        {
            command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
            if (startDate.HasValue)
                command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
            if (endDate.HasValue)
                command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var device = reader.GetString(0);
                var count = reader.GetInt64(1);
                viewsByDevice[device] = count;
            }
        }

        // Get daily views
        var dailyViews = new List<DailyViewCount>();
        var dailyQuery = $@"
            SELECT DATE(""ViewedAt"") AS ""Date"", COUNT(*)::bigint AS ""Count""
            FROM public.""ViewTelemetries""
            WHERE {whereClause}
            GROUP BY DATE(""ViewedAt"")
            ORDER BY DATE(""ViewedAt"");";

        await using (var command = new NpgsqlCommand(dailyQuery, connection))
        {
            command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
            if (startDate.HasValue)
                command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
            if (endDate.HasValue)
                command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var date = DateOnly.FromDateTime(reader.GetDateTime(0));
                var count = reader.GetInt64(1);
                dailyViews.Add(new DailyViewCount(date, count));
            }
        }

        return new ProfileViewStatsResponse(
            totalViews,
            uniqueVisitors,
            viewsBySource,
            viewsByCountry,
            viewsByDevice,
            dailyViews,
            avgDuration
        );
    }

    public async Task<IReadOnlyList<SourceBreakdownResponse>> GetSourceBreakdownAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default)
    {
        var results = new List<SourceBreakdownResponse>();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string> { @"""ProfileUserId"" = @profileUserId" };
        if (startDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" >= @startDate");
        if (endDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" <= @endDate");
        var whereClause = string.Join(" AND ", whereClauses);

        var query = $@"
            WITH total AS (
                SELECT COUNT(*)::bigint AS ""Total""
                FROM public.""ViewTelemetries""
                WHERE {whereClause}
            )
            SELECT 
                COALESCE(""Source"", 'unknown') AS ""Source"",
                COUNT(*)::bigint AS ""Count"",
                (COUNT(*)::float / total.""Total"" * 100) AS ""Percentage""
            FROM public.""ViewTelemetries"", total
            WHERE {whereClause}
            GROUP BY ""Source"", total.""Total""
            ORDER BY ""Count"" DESC;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
        if (startDate.HasValue)
            command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
        if (endDate.HasValue)
            command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var source = reader.GetString(0);
            var count = reader.GetInt64(1);
            var percentage = reader.GetDouble(2);
            results.Add(new SourceBreakdownResponse(source, count, percentage));
        }

        return results;
    }

    public async Task<IReadOnlyList<GeographicDistributionResponse>> GetGeographicDistributionAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default)
    {
        var results = new List<GeographicDistributionResponse>();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var whereClauses = new List<string> { @"""ProfileUserId"" = @profileUserId" };
        if (startDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" >= @startDate");
        if (endDate.HasValue)
            whereClauses.Add(@"""ViewedAt"" <= @endDate");
        var whereClause = string.Join(" AND ", whereClauses);

        var query = $@"
            WITH total AS (
                SELECT COUNT(*)::bigint AS ""Total""
                FROM public.""ViewTelemetries""
                WHERE {whereClause}
            )
            SELECT 
                COALESCE(""Country"", 'unknown') AS ""Country"",
                ""City"",
                COUNT(*)::bigint AS ""Count"",
                (COUNT(*)::float / total.""Total"" * 100) AS ""Percentage""
            FROM public.""ViewTelemetries"", total
            WHERE {whereClause}
            GROUP BY ""Country"", ""City"", total.""Total""
            ORDER BY ""Count"" DESC
            LIMIT 50;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;
        if (startDate.HasValue)
            command.Parameters.Add("startDate", NpgsqlDbType.TimestampTz).Value = startDate.Value;
        if (endDate.HasValue)
            command.Parameters.Add("endDate", NpgsqlDbType.TimestampTz).Value = endDate.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var country = reader.GetString(0);
            var city = reader.IsDBNull(1) ? null : reader.GetString(1);
            var count = reader.GetInt64(2);
            var percentage = reader.GetDouble(3);
            results.Add(new GeographicDistributionResponse(country, city, count, percentage));
        }

        return results;
    }

    public async Task<long> GetTotalViewCountAsync(Guid profileUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT COUNT(*)::bigint
            FROM public.""ViewTelemetries""
            WHERE ""ProfileUserId"" = @profileUserId;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;

        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    public async Task<long> GetUniqueVisitorCountAsync(Guid profileUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT COUNT(DISTINCT ""Fingerprint"")::bigint
            FROM public.""ViewTelemetries""
            WHERE ""ProfileUserId"" = @profileUserId AND ""Fingerprint"" IS NOT NULL;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("profileUserId", NpgsqlDbType.Uuid).Value = profileUserId;

        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DefaultConnectionName}' not found");
    }
}
