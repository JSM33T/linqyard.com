using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Linkyard.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private const string DefaultConnectionName = "DefaultConnection";

    private readonly ILogger<AnalyticsRepository> _logger;
    private readonly IConfiguration _configuration;

    public AnalyticsRepository(ILogger<AnalyticsRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task RecordLinkClickAsync(RecordLinkClickRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recording link click for link {LinkId}", request.LinkId);

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string commandText = @"
            INSERT INTO public.""Analytics"" (
                ""Id"", ""LinkId"", ""UserId"", ""Fingerprint"", ""Latitude"", ""Longitude"",
                ""Accuracy"", ""UserAgent"", ""IpAddress"", ""At""
            ) VALUES (
                @id, @linkId, @userId, @fingerprint, @latitude, @longitude,
                @accuracy, @userAgent, @ipAddress, @at
            );";

        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = request.Id;
        command.Parameters.Add("linkId", NpgsqlDbType.Uuid).Value = request.LinkId;
        command.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = (object?)request.UserId ?? DBNull.Value;
        command.Parameters.Add("fingerprint", NpgsqlDbType.Text).Value = (object?)request.Fingerprint ?? DBNull.Value;
        command.Parameters.Add("latitude", NpgsqlDbType.Double).Value = (object?)request.Latitude ?? DBNull.Value;
        command.Parameters.Add("longitude", NpgsqlDbType.Double).Value = (object?)request.Longitude ?? DBNull.Value;
        command.Parameters.Add("accuracy", NpgsqlDbType.Double).Value = (object?)request.Accuracy ?? DBNull.Value;
        command.Parameters.Add("userAgent", NpgsqlDbType.Text).Value = (object?)request.UserAgent ?? DBNull.Value;
        command.Parameters.Add("ipAddress", NpgsqlDbType.Inet).Value = (object?)request.IpAddress ?? DBNull.Value;
        command.Parameters.Add("at", NpgsqlDbType.TimestampTz).Value = request.At;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LinkClickCountResponse>> GetLinkClickCountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var results = new List<LinkClickCountResponse>();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT a.""LinkId"", COUNT(*)::bigint AS ""Clicks""
            FROM public.""Analytics"" AS a
            INNER JOIN public.""Links"" AS l ON l.""Id"" = a.""LinkId""
            WHERE l.""UserId"" = @userId
            GROUP BY a.""LinkId"";";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var linkId = reader.GetGuid(0);
            var clicks = reader.GetInt64(1);
            results.Add(new LinkClickCountResponse(linkId, clicks));
        }

        return results;
    }

    public async Task<IReadOnlyList<AnalyticsEventResponse>?> GetLinkEventsForUserAsync(Guid userId, Guid linkId, int take, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string ownershipQuery = @"
            SELECT 1
            FROM public.""Links""
            WHERE ""Id"" = @linkId AND ""UserId"" = @userId
            LIMIT 1;";

        await using (var ownershipCommand = new NpgsqlCommand(ownershipQuery, connection))
        {
            ownershipCommand.Parameters.Add("linkId", NpgsqlDbType.Uuid).Value = linkId;
            ownershipCommand.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

            var ownsLink = await ownershipCommand.ExecuteScalarAsync(cancellationToken);
            if (ownsLink is null)
            {
                return null;
            }
        }

        const string eventsQuery = @"
            SELECT ""Id"", ""At"", ""Fingerprint"", ""Latitude"", ""Longitude"", ""Accuracy"", ""UserAgent""
            FROM public.""Analytics""
            WHERE ""LinkId"" = @linkId
            ORDER BY ""At"" DESC
            LIMIT @take;";

        var responses = new List<AnalyticsEventResponse>();

        await using var eventsCommand = new NpgsqlCommand(eventsQuery, connection);
        eventsCommand.Parameters.Add("linkId", NpgsqlDbType.Uuid).Value = linkId;
        eventsCommand.Parameters.Add("take", NpgsqlDbType.Integer).Value = take;

        await using var reader = await eventsCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var at = reader.GetFieldValue<DateTimeOffset>(1);
            var fingerprint = reader.IsDBNull(2) ? null : reader.GetString(2);
            var latitude = reader.IsDBNull(3) ? (double?)null : reader.GetDouble(3);
            var longitude = reader.IsDBNull(4) ? (double?)null : reader.GetDouble(4);
            var accuracy = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5);
            var userAgent = reader.IsDBNull(6) ? null : reader.GetString(6);

            responses.Add(new AnalyticsEventResponse(id, at, fingerprint, latitude, longitude, accuracy, userAgent));
        }

        return responses;
    }

    public async Task<long> GetClickCountAsync(Guid userId, DateTimeOffset fromInclusive, DateTimeOffset toInclusive, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT COUNT(*)::bigint
            FROM public.""Analytics"" AS a
            INNER JOIN public.""Links"" AS l ON l.""Id"" = a.""LinkId""
            WHERE l.""UserId"" = @userId
              AND a.""At"" >= @from
              AND a.""At"" <= @to;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;
        command.Parameters.Add("from", NpgsqlDbType.TimestampTz).Value = fromInclusive;
        command.Parameters.Add("to", NpgsqlDbType.TimestampTz).Value = toInclusive;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long count ? count : Convert.ToInt64(result);
    }

    public async Task<IReadOnlyList<string?>> GetUserAgentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userAgents = new List<string?>();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT a.""UserAgent""
            FROM public.""Analytics"" AS a
            INNER JOIN public.""Links"" AS l ON l.""Id"" = a.""LinkId""
            WHERE l.""UserId"" = @userId;";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add("userId", NpgsqlDbType.Uuid).Value = userId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var userAgent = reader.IsDBNull(0) ? null : reader.GetString(0);
            userAgents.Add(userAgent);
        }

        return userAgents;
    }

    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString(DefaultConnectionName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        const string message = "Database connection string not configured";
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }
}
