# Unverified Account Cleanup

## Overview

This feature enables automatic reclaiming of usernames and email addresses from accounts that remain unverified after a configurable grace period. This prevents permanently locking credentials that were registered but never verified.

## How It Works

### Background Service

A background service (`UnverifiedAccountCleanupService`) runs periodically to clean up stale unverified accounts:

- **Default interval**: Every 24 hours (configurable)
- **Grace period**: 30 days by default (configurable)
- **Cleanup process**: Removes accounts where `EmailVerified = false` and `CreatedAt < (Now - GracePeriod)`

### On-Demand Cleanup During Registration

When a user attempts to register with an email or username:

1. System checks if the email/username already exists
2. If it exists and the account is:
   - **Verified**: Returns a conflict error (email/username taken)
   - **Unverified and within grace period**: Returns a conflict error (to prevent abuse)
   - **Unverified and past grace period**: Deletes the old account and allows registration

## Configuration

Add these settings to your `appsettings.json` under the `Auth` section:

```json
{
  "Auth": {
    "UnverifiedAccountGracePeriodDays": 30,
    "UnverifiedAccountCleanupIntervalHours": 24
  }
}
```

### Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| `UnverifiedAccountGracePeriodDays` | 30 | Number of days before an unverified account can be reclaimed |
| `UnverifiedAccountCleanupIntervalHours` | 24 | How often the background cleanup service runs |

## Benefits

1. **Prevents credential hoarding**: Users can't register and hold usernames/emails indefinitely without verification
2. **Improves user experience**: Legitimate users aren't blocked by abandoned registration attempts
3. **Automatic cleanup**: No manual intervention needed to reclaim credentials
4. **Configurable**: Grace period can be adjusted based on your needs
5. **Safe**: Only affects truly unverified accounts, verified accounts are never touched

## Database Impact

- Uses cascade delete, so related records (sessions, tokens, etc.) are automatically cleaned up
- Minimal performance impact - runs once per day by default and only queries/deletes old unverified records

## Logging

The service logs important events:

- When cleanup starts and how many accounts were found
- Each account removal (at Info level)
- Any errors during cleanup (at Error level)

Example log entries:
```
[INF] Starting cleanup of unverified accounts older than 2024-12-15T10:30:00Z (30 days)
[INF] Found 3 stale unverified accounts to remove
[INF] Removing stale unverified account for email user@example.com (created 2024-11-10T08:22:00Z)
[INF] Successfully removed 3 stale unverified accounts
```

## Security Considerations

- **Grace period**: The 30-day default provides a reasonable balance between allowing users time to verify and preventing credential squatting
- **Cascade delete**: Ensures no orphaned records remain
- **Email notifications**: Users receive verification emails immediately, so 30 days is ample time
- **No data loss**: Only affects accounts that never completed verification

## Monitoring

Monitor these metrics to ensure the feature is working correctly:

1. Number of accounts cleaned up per day (should be low in steady state)
2. Cleanup service errors (should be zero)
3. User complaints about "taken" usernames (should decrease)

## Troubleshooting

### Background service not running

Check application logs for:
```
Unverified Account Cleanup Service started
```

If missing, verify the service is registered in `Program.cs`:
```csharp
builder.Services.AddHostedService<UnverifiedAccountCleanupService>();
```

### Accounts not being cleaned up

1. Verify configuration values in `appsettings.json`
2. Check that accounts are actually older than the grace period
3. Look for errors in logs around cleanup time
4. Ensure the `CreatedAt` field is set correctly on User entities

### Grace period seems wrong

Remember the calculation:
```
CutoffDate = Now - GracePeriodDays
AccountsToDelete = Users where EmailVerified=false AND CreatedAt < CutoffDate
```

So a 30-day grace period means accounts created 30 days ago or earlier will be deleted.
