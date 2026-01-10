using System;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Linqyard.Worker.Services;

public class AccountCleanupService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AccountCleanupService> _logger;
    private readonly TimeSpan _unverifiedAgeThreshold;

    public AccountCleanupService(IUserRepository userRepository, ILogger<AccountCleanupService> logger, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _logger = logger;

        var cfg = configuration.GetSection("AccountCleanup:UnverifiedAccountAge").Value;
        _unverifiedAgeThreshold = string.IsNullOrEmpty(cfg) ? TimeSpan.FromDays(1) : TimeSpan.Parse(cfg);
    }

    public async Task CleanupUnverifiedUsersAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - _unverifiedAgeThreshold;
        _logger.LogInformation("AccountCleanup: looking for unverified users created before {Cutoff}", cutoff);

        var ids = await _userRepository.GetUnverifiedUserIdsCreatedBeforeAsync(cutoff, cancellationToken);
        if (ids == null || ids.Count == 0)
        {
            _logger.LogInformation("AccountCleanup: no unverified users found");
            return;
        }

        _logger.LogInformation("AccountCleanup: found {Count} unverified users to delete", ids.Count);

        foreach (var id in ids)
        {
            try
            {
                await _userRepository.SoftDeleteUserAsync(id, cancellationToken);
                _logger.LogInformation("AccountCleanup: soft-deleted user {UserId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccountCleanup: failed to delete user {UserId}", id);
            }
        }
    }
}
