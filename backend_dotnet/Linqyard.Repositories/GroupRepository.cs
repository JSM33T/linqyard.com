using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Linqyard.Entities;
using Linqyard.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Linqyard.Repositories;

public sealed class GroupRepository(LinqyardDbContext dbContext, ILogger<GroupRepository> logger) : IGroupRepository
{
    private readonly LinqyardDbContext _dbContext = dbContext;
    private readonly ILogger<GroupRepository> _logger = logger;

    public async Task<IReadOnlyList<LinkGroupResponse>> GetGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LinkGroups
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Sequence)
            .Select(group => new LinkGroupResponse(
                group.Id,
                group.Name,
                group.Description,
                group.Sequence,
                group.IsActive,
                new List<LinkSummary>()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LinkGroupResponse>> GetGroupsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var usernameNorm = (username ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(usernameNorm))
        {
            throw new GroupNotFoundException("User not found.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameNorm, cancellationToken);

        if (user is null)
        {
            throw new GroupNotFoundException("User not found.");
        }

        return await GetGroupsAsync(user.Id, cancellationToken);
    }

    public async Task<int?> GetActiveTierIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.UserTiers
            .AsNoTracking()
            .Where(ut => ut.UserId == userId &&
                         ut.IsActive &&
                         ut.ActiveFrom <= now &&
                         (ut.ActiveUntil == null || ut.ActiveUntil >= now))
            .OrderByDescending(ut => ut.ActiveFrom)
            .Select(ut => (int?)ut.TierId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetUserGroupCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LinkGroups
            .CountAsync(g => g.UserId == userId, cancellationToken);
    }

    public async Task<LinkGroupResponse> CreateGroupAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new GroupValidationException("Group name is required.");
        }

        var activeTierId = await GetActiveTierIdAsync(userId, cancellationToken) ?? (int)TierType.Free;
        if (activeTierId == (int)TierType.Free)
        {
            var existingGroupsCount = await GetUserGroupCountAsync(userId, cancellationToken);
            if (existingGroupsCount >= 2)
            {
                throw new GroupLimitExceededException(
                    "Free tier users can create a maximum of 2 groups. Please upgrade to create more groups.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var group = new LinkGroup
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Sequence = request.Sequence ?? 0,
            IsActive = request.IsActive ?? true,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.LinkGroups.AddAsync(group, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LinkGroupResponse(
            group.Id,
            group.Name,
            group.Description,
            group.Sequence,
            group.IsActive,
            new List<LinkSummary>());
    }

    public async Task<LinkGroupResponse> UpdateGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.LinkGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            throw new GroupNotFoundException("Group not found.");
        }

        if (!isAdmin && group.UserId != userId)
        {
            throw new GroupForbiddenException("You do not have permission to edit this group.");
        }

        if (request.Name is not null)
        {
            group.Name = string.IsNullOrWhiteSpace(request.Name) ? group.Name : request.Name.Trim();
        }

        if (request.Description is not null)
        {
            group.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Sequence.HasValue)
        {
            group.Sequence = request.Sequence.Value;
        }

        if (request.IsActive.HasValue)
        {
            group.IsActive = request.IsActive.Value;
        }

        group.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LinkGroupResponse(
            group.Id,
            group.Name,
            group.Description,
            group.Sequence,
            group.IsActive,
            new List<LinkSummary>());
    }

    public async Task<LinkGroupResponse> UpdateGroupStatusAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.LinkGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            throw new GroupNotFoundException("Group not found.");
        }

        if (!isAdmin && group.UserId != userId)
        {
            throw new GroupForbiddenException("You do not have permission to edit this group.");
        }

        group.IsActive = isActive;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LinkGroupResponse(
            group.Id,
            group.Name,
            group.Description,
            group.Sequence,
            group.IsActive,
            new List<LinkSummary>());
    }

    public async Task DeleteGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.LinkGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            throw new GroupNotFoundException("Group not found.");
        }

        if (!isAdmin && group.UserId != userId)
        {
            throw new GroupForbiddenException("You do not have permission to delete this group.");
        }

        var links = await _dbContext.Links.Where(l => l.GroupId == groupId).ToListAsync(cancellationToken);
        foreach (var link in links)
        {
            link.GroupId = null;
        }

        _dbContext.LinkGroups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GroupResequenceResult> ResequenceGroupsAsync(
        Guid userId,
        IReadOnlyList<GroupResequenceItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            throw new GroupValidationException("No items provided.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            foreach (var item in items)
            {
                var rowsAffected = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"LinkGroups\" SET \"Sequence\" = {item.Sequence}, \"UpdatedAt\" = {DateTimeOffset.UtcNow} WHERE \"Id\" = {item.Id} AND \"UserId\" = {userId}",
                    cancellationToken);

                _logger.LogInformation(
                    "Group resequence update: GroupId={GroupId}, Sequence={Sequence}, RowsAffected={RowsAffected}",
                    item.Id,
                    item.Sequence,
                    rowsAffected);
            }
        });

        var groupIds = items.Select(i => i.Id).ToList();
        var finalGroups = await _dbContext.LinkGroups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id) && g.UserId == userId)
            .OrderBy(g => g.Sequence)
            .Select(g => new GroupSequenceStateResponse(g.Id, g.Sequence, g.Name))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Group resequence final state: {@FinalGroups}", finalGroups);

        return new GroupResequenceResult(
            "Groups resequenced exactly as specified",
            finalGroups);
    }
}
