# Linqyard Worker - Geolocation Enrichment Service

## Overview

This background worker service automatically enriches Analytics and ViewTelemetries records by filling in missing country and city information based on IP addresses stored in the database.

## Features

- **Automatic Enrichment**: Periodically scans for records with IP addresses but missing geolocation data
- **Batch Processing**: Processes records in configurable batches to manage resources
- **Rate Limiting**: Built-in delays between API calls to avoid rate limiting
- **Caching**: Uses in-memory caching to avoid redundant API calls for the same IPs
- **Dual Table Support**: Enriches both `Analytics` and `ViewTelemetries` tables
- **Configurable**: Fully customizable through appsettings.json

## How It Works

1. **Discovery**: The worker queries the database for records where:
   - `IpAddress` is NOT NULL
   - `Country` IS NULL OR `City` IS NULL

2. **Enrichment**: For each discovered IP address:
   - Checks memory cache first
   - If not cached, calls ipapi.co API to get geolocation data
   - Updates the record with: Country, City, Latitude, Longitude, Accuracy
   - Caches the result for future use

3. **Update**: Uses `COALESCE` to only fill empty fields, preserving existing data

4. **Scheduling**: Runs on a configurable interval (default: every 6 hours)

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your PostgreSQL connection string"
  },
  "IpGeolocation": {
    "Enabled": true,
    "BaseUrl": "https://ipapi.co",
    "CacheDuration": "06:00:00",
    "DefaultAccuracyMeters": 50000,
    "SkipPrivateRanges": true,
    "ApiKey": ""
  },
  "GeolocationEnrichment": {
    "RunInterval": "06:00:00",
    "BatchSize": 100,
    "DelayBetweenRecords": "00:00:00.200",
    "ProcessAnalytics": true,
    "ProcessViewTelemetries": true
  }
}
```

### Configuration Options

#### IpGeolocation
- **Enabled**: Enable/disable IP geolocation lookups
- **BaseUrl**: API endpoint (default: ipapi.co)
- **CacheDuration**: How long to cache IP lookups
- **DefaultAccuracyMeters**: Fallback accuracy when API doesn't provide it
- **SkipPrivateRanges**: Skip private IP addresses (127.x, 10.x, 192.168.x, etc.)
- **ApiKey**: Optional API key for premium features

#### GeolocationEnrichment
- **RunInterval**: How often to run the enrichment job (e.g., "06:00:00" for 6 hours)
- **BatchSize**: Maximum records to process per run
- **DelayBetweenRecords**: Delay between processing each record (helps with rate limiting)
- **ProcessAnalytics**: Enable/disable Analytics table enrichment
- **ProcessViewTelemetries**: Enable/disable ViewTelemetries table enrichment

## Running the Worker

### Development
```bash
cd backend_dotnet/Linqyard.Worker
dotnet run --launch-profile "http"
```

### Production
```bash
dotnet run --environment Production
```

### Docker
The worker includes a Dockerfile for containerized deployment:
```bash
docker build -t linqyard-worker .
docker run -d linqyard-worker
```

## API Rate Limits

The free tier of ipapi.co allows:
- 1,000 requests per day
- 30,000 requests per month

To stay within limits:
- Set appropriate `DelayBetweenRecords` (200ms = ~5 requests/second max)
- Set reasonable `BatchSize` (100 records = 100 API calls if all IPs are unique)
- Consider running less frequently (every 12-24 hours)
- Use caching effectively (default 6 hours)

For higher volume, consider:
- Upgrading to a paid plan
- Using alternative providers (MaxMind, IP2Location, etc.)
- Implementing your own geolocation database

## Database Schema

### Analytics Table
```sql
CREATE TABLE "Analytics" (
    "Id" uuid PRIMARY KEY,
    "IpAddress" inet,
    "Country" varchar(100),
    "City" varchar(100),
    "Latitude" double precision,
    "Longitude" double precision,
    "Accuracy" double precision,
    ...
);
```

### ViewTelemetries Table
```sql
CREATE TABLE "ViewTelemetries" (
    "Id" uuid PRIMARY KEY,
    "IpAddress" inet,
    "Country" varchar(2),
    "City" varchar(100),
    "Latitude" double precision,
    "Longitude" double precision,
    "Accuracy" double precision,
    ...
);
```

## Monitoring

The worker logs important events:
- **Info**: Job start/completion, record counts
- **Debug**: Individual record processing (enable in Development)
- **Warning**: Failed API calls, missing configuration
- **Error**: Critical failures

Example log output:
```
[12:00:00 INF] Geolocation Enrichment Worker started. Will run every 06:00:00
[12:00:00 INF] Starting geolocation enrichment cycle
[12:00:01 INF] Enriching Analytics table
[12:00:01 INF] Found 45 Analytics records to enrich
[12:00:15 INF] Enriching ViewTelemetries table
[12:00:16 INF] Found 23 ViewTelemetries records to enrich
[12:00:25 INF] Geolocation enrichment completed. Analytics: 45, ViewTelemetries: 23
[12:00:25 INF] Next run in 06:00:00
```

## Troubleshooting

### No records are being enriched
- Check database connection string
- Verify records exist with `IpAddress IS NOT NULL AND (Country IS NULL OR City IS NULL)`
- Check if `IpGeolocation.Enabled` is `true`
- Look for errors in logs

### Rate limiting errors
- Increase `DelayBetweenRecords`
- Decrease `BatchSize`
- Increase `RunInterval`

### Worker not running periodically
- Check `RunInterval` configuration
- Ensure worker process is running
- Check for exceptions in logs

## Future Enhancements

- [ ] Add support for bulk IP lookup APIs
- [ ] Implement retry logic with exponential backoff
- [ ] Add metrics/telemetry for monitoring
- [ ] Support for multiple geolocation providers
- [ ] Database-driven scheduling (vs fixed interval)
- [ ] Web dashboard for monitoring enrichment status
