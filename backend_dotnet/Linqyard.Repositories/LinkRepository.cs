using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Data;
using Linqyard.Entities;
using Linqyard.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqyard.Repositories;

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message) { }
}

public sealed class LinkRepository : ILinkRepository
{
    private readonly LinqyardDbContext _db;
    private readonly ILogger<LinkRepository> _logger;

    public LinkRepository(LinqyardDbContext db, ILogger<LinkRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // --------------------------
    // Queries
    // --------------------------

    public async Task<LinksGroupedResponse> GetLinksForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Load groups and links in two queries (fast, simple; both AsNoTracking)
        var groups = await _db.LinkGroups
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Sequence)
            .ToListAsync(ct);

        var links = await _db.Links
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Sequence)
            .ToListAsync(ct);

        return MapGroupsAndLinks(groups, links);
    }

    public async Task<LinksGroupedResponse?> GetLinksByUsernameAsync(string username, CancellationToken ct = default)
    {
        var norm = (username ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(norm)) return null;

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == norm, ct);

        if (user is null) return null;

        var groups = await _db.LinkGroups
            .AsNoTracking()
            .Where(g => g.UserId == user.Id)
            .OrderBy(g => g.Sequence)
            .ToListAsync(ct);

        var links = await _db.Links
            .AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .OrderBy(l => l.Sequence)
            .ToListAsync(ct);

        return MapGroupsAndLinks(groups, links);
    }

    private static LinksGroupedResponse MapGroupsAndLinks(List<LinkGroup> groups, List<Link> links)
    {
        var grouped = groups
            .Select(g => new LinkGroupResponse(
                Id: g.Id,
                Name: g.Name,
                Description: g.Description,
                Sequence: g.Sequence,
                IsActive: g.IsActive,
                Links: links
                    .Where(l => l.GroupId == g.Id)
                    .OrderBy(l => l.Sequence)
                    .Select(MapSummary)
                    .ToList()
            ))
            .ToList();

        var ungrouped = new LinkGroupResponse(
            Id: Guid.Empty,
            Name: "Ungrouped",
            Description: null,
            Sequence: 0,
            IsActive: true,
            Links: links
                .Where(l => l.GroupId == null)
                .OrderBy(l => l.Sequence)
                .Select(MapSummary)
                .ToList()
        );

        return new LinksGroupedResponse(grouped, ungrouped);
    }

    // --------------------------
    // Create
    // --------------------------

    public async Task<LinkSummary> CreateLinkAsync(Guid userId, CreateLinkRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("Name and Url are required.");

        if (!IsValidAbsoluteUrl(request.Url))
            throw new ArgumentException("Url is not a valid absolute URL.");

        // Free plan enforcement (12 links)
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Tier)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user?.TierId == (int)TierType.Free)
        {
            var existingCount = await _db.Links.LongCountAsync(l => l.UserId == userId, ct);
            if (existingCount >= 12)
                throw new InvalidOperationException("Free tier users can create a maximum of 12 links. Please upgrade to create more links.");
        }

        // Group validation (Guid.Empty => ungroup)
        Guid? groupId = NormalizeGroupId(request.GroupId);
        if (groupId.HasValue)
        {
            var groupOk = await _db.LinkGroups
                .AnyAsync(g => g.Id == groupId.Value && g.UserId == userId, ct);
            if (!groupOk) throw new ArgumentException("Specified group does not exist or does not belong to you.");
        }

        var now = DateTimeOffset.UtcNow;
        var link = new Link
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            UserId = userId,
            GroupId = groupId,
            Sequence = request.Sequence ?? 0,
            IsActive = request.IsActive ?? true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Links.Add(link);
        await _db.SaveChangesAsync(ct);

        return MapSummary(link);
    }

    // --------------------------
    // Edit (partial update)
    // --------------------------

    public async Task<LinkSummary?> EditLinkAsync(Guid editorUserId, Guid linkId, EditLinkRequest request, bool isAdmin, CancellationToken ct = default)
    {
        var link = await _db.Links.FirstOrDefaultAsync(l => l.Id == linkId, ct);
        if (link is null) return null;

        var isOwner = link.UserId == editorUserId;
        if (!isOwner && !isAdmin)
            throw new ForbiddenAccessException("You do not have permission to edit this link.");

        // Apply partials
        if (!string.IsNullOrWhiteSpace(request.Name))
            link.Name = request.Name.Trim();

        if (!string.IsNullOrWhiteSpace(request.Url))
        {
            if (!IsValidAbsoluteUrl(request.Url))
                throw new ArgumentException("Url is not a valid absolute URL.");
            link.Url = request.Url.Trim();
        }

        if (request.Description is not null)
            link.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (request is { GroupId: { } })
        {
            // Explicitly provided; Guid.Empty or null => ungroup
            var normalized = NormalizeGroupId(request.GroupId);
            if (normalized.HasValue)
            {
                var groupOk = await _db.LinkGroups
                    .AnyAsync(g => g.Id == normalized.Value && (g.UserId == editorUserId || isAdmin), ct);
                if (!groupOk)
                    throw new ArgumentException("Specified group does not exist or does not belong to you.");
            }
            link.GroupId = normalized;
        }

        if (request.Sequence.HasValue)
            link.Sequence = request.Sequence.Value;

        if (request.IsActive.HasValue)
            link.IsActive = request.IsActive.Value;

        link.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapSummary(link);
    }

    // --------------------------
    // Resequence (exact)
    // --------------------------

    public async Task<IReadOnlyList<LinkSequenceState>> ResequenceAsync(Guid userId, IReadOnlyList<ResequenceItem> items, CancellationToken ct = default)
    {
        if (items is null || items.Count == 0)
            return Array.Empty<LinkSequenceState>();

        // Load all affected links for ownership validation
        var ids = items.Select(i => i.Id).ToList();
        var links = await _db.Links.Where(l => ids.Contains(l.Id)).ToListAsync(ct);

        // Ensure all belong to user
        var invalid = links.Where(l => l.UserId != userId).Select(l => l.Id).ToList();
        if (invalid.Count > 0)
            throw new ForbiddenAccessException($"One or more links do not belong to the user: {string.Join(",", invalid)}");

        // Pre-validate groups (when provided and not null/empty)
        var groupIds = items.Select(i => NormalizeGroupId(i.GroupId)).Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();
        if (groupIds.Count > 0)
        {
            var okGroups = await _db.LinkGroups
                .Where(g => g.UserId == userId && groupIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync(ct);

            var missing = groupIds.Except(okGroups).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"One or more groups do not exist or do not belong to you: {string.Join(",", missing)}");
        }

        // Apply updates EXACTLY as requested in a transaction
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var now = DateTimeOffset.UtcNow;

                // Update in-memory entities (EF) and let EF save; no concurrency surprises if using default optimistic concurrency.
                var byId = links.ToDictionary(l => l.Id);
                foreach (var item in items)
                {
                    var link = byId[item.Id];
                    link.Sequence = item.Sequence;
                    link.GroupId = NormalizeGroupId(item.GroupId);
                    link.UpdatedAt = now;
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        // Return final states (ordered for readability)
        var final = await _db.Links
            .AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Select(l => new LinkSequenceState(l.Id, l.Sequence, l.GroupId))
            .OrderBy(l => l.GroupId).ThenBy(l => l.Sequence)
            .ToListAsync(ct);

        return final;
    }

    // --------------------------
    // Delete
    // --------------------------

    public async Task<DeleteLinkResult> DeleteLinkAsync(Guid requesterUserId, Guid linkId, bool isAdmin, CancellationToken ct = default)
    {
        var link = await _db.Links.FirstOrDefaultAsync(l => l.Id == linkId, ct);
        if (link is null) return DeleteLinkResult.NotFound;

        var isOwner = link.UserId == requesterUserId;
        if (!isOwner && !isAdmin) return DeleteLinkResult.Forbidden;

        _db.Links.Remove(link);
        await _db.SaveChangesAsync(ct);
        return DeleteLinkResult.Deleted;
    }

    // --------------------------
    // Helpers
    // --------------------------

    private static LinkSummary MapSummary(Link l) => new(
        Id: l.Id,
        Name: l.Name,
        Url: l.Url,
        Description: l.Description,
        IsActive: l.IsActive,
        Sequence: l.Sequence,
        GroupId: l.GroupId,
        CreatedAt: l.CreatedAt,
        UpdatedAt: l.UpdatedAt
    );

    private static Guid? NormalizeGroupId(Guid? groupId)
    {
        if (!groupId.HasValue) return null;
        if (groupId.Value == Guid.Empty) return null;
        return groupId;
    }

    private static bool IsValidAbsoluteUrl(string url)
    {
        // Keep it practical: absolute URI + HTTP(S) only
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}