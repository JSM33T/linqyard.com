using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Contracts;
using Linqyard.Entities;
using Linqyard.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Linqyard.Data;

namespace Linqyard.Api.Controllers;

// DTO for group resequence payload
public record GroupResequenceItemRequest(Guid Id, int Sequence);

[Route("group")]
[ApiController]
public sealed class GroupsController : BaseApiController
{
    private readonly LinqyardDbContext _context;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(ILogger<GroupsController> logger, LinqyardDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LinkGroupResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            var groups = await _context.LinkGroups
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Sequence)
                .Select(g => new LinkGroupResponse(
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Sequence,
                    g.IsActive,
                    new List<LinkSummary>()
                ))
                .ToListAsync(cancellationToken);

            return OkEnvelope(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving groups");
        }
    }

    /// <summary>
    /// Get groups for a public username (anonymous access).
    /// </summary>
    [HttpGet("user/{username}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LinkGroupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupsByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var usernameNorm = (username ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(usernameNorm)) return NotFoundProblem("User not found");

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == usernameNorm, cancellationToken);
            if (user == null) return NotFoundProblem("User not found");

            var userId = user.Id;

            var groups = await _context.LinkGroups
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Sequence)
                .Select(g => new LinkGroupResponse(
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Sequence,
                    g.IsActive,
                    new List<LinkSummary>()
                ))
                .ToListAsync(cancellationToken);

            return OkEnvelope(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving groups");
        }
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(ApiResponse<LinkGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating link group with CorrelationId {CorrelationId}", CorrelationId);
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequestProblem("Name is required");

            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            // Check tier-based limits for free tier users
            var activeTierId = await GetActiveTierIdAsync(userId, cancellationToken) ?? (int)TierType.Free;

            if (activeTierId == (int)TierType.Free)
            {
                var existingGroupsCount = await _context.LinkGroups
                    .CountAsync(g => g.UserId == userId, cancellationToken);

                if (existingGroupsCount >= 2)
                {
                    return BadRequestProblem("Free tier users can create a maximum of 2 groups. Please upgrade to create more groups.");
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

            _context.LinkGroups.Add(group);
            await _context.SaveChangesAsync(cancellationToken);

            var resp = new LinkGroupResponse(group.Id, group.Name, group.Description, group.Sequence, group.IsActive, new List<LinkSummary>());
            return OkEnvelope(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while creating the group");
        }
    }

    [HttpPost("{id:guid}/edit")]
    [ProducesResponseType(typeof(ApiResponse<LinkGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditGroup(Guid id, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editing group {GroupId} with CorrelationId {CorrelationId}", id, CorrelationId);
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            var group = await _context.LinkGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (group == null) return NotFoundProblem("Group not found");

            var isOwner = group.UserId == userId;
            var isAdmin = User.IsInRole("admin");
            if (!isOwner && !isAdmin) return ForbiddenProblem("You do not have permission to edit this group");

            if (request.Name != null) group.Name = string.IsNullOrWhiteSpace(request.Name) ? group.Name : request.Name.Trim();
            if (request.Description != null) group.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (request.Sequence.HasValue) group.Sequence = request.Sequence.Value;
            if (request.IsActive.HasValue) group.IsActive = request.IsActive.Value;

            group.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var resp = new LinkGroupResponse(group.Id, group.Name, group.Description, group.Sequence, group.IsActive, new List<LinkSummary>());
            return OkEnvelope(resp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing group {GroupId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while editing the group");
        }
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting group {GroupId} with CorrelationId {CorrelationId}", id, CorrelationId);
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            var group = await _context.LinkGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
            if (group == null) return NotFoundProblem("Group not found");

            var isOwner = group.UserId == userId;
            var isAdmin = User.IsInRole("admin");
            if (!isOwner && !isAdmin) return ForbiddenProblem("You do not have permission to delete this group");

            // Option: decide whether to delete associated links or set their GroupId to null. We'll set GroupId to null.
            var links = await _context.Links.Where(l => l.GroupId == id).ToListAsync(cancellationToken);
            foreach (var l in links) l.GroupId = null;

            _context.LinkGroups.Remove(group);
            await _context.SaveChangesAsync(cancellationToken);

            return OkEnvelope(new { Message = "Group deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while deleting the group");
        }
    }

    /// <summary>
    /// Resequence groups for the current user. POST-only.
    /// Body: [{ "id": "...", "sequence": 0 }, ...]
    /// </summary>
    [HttpPost("resequence")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResequenceGroups([FromBody] List<GroupResequenceItemRequest> items, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GROUP RESEQUENCE START: {Count} items", items?.Count ?? 0);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        if (items == null || items.Count == 0)
            return BadRequestProblem("No items provided");

        try
        {
            // Use execution strategy to handle retries for transactional operations
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // Process each group exactly as sent - use EXACT sequence specified
                foreach (var item in items)
                {
                    var exactSequence = item.Sequence;
                    
                    _logger.LogInformation("Setting group {Id} to EXACT sequence {Seq}", 
                        item.Id, exactSequence);
                        
                    // Direct SQL update - set sequence for user's groups only
                    var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE \"LinkGroups\" SET \"Sequence\" = {0}, \"UpdatedAt\" = {1} WHERE \"Id\" = {2} AND \"UserId\" = {3}",
                        exactSequence, DateTimeOffset.UtcNow, item.Id, userId);
                    
                    _logger.LogInformation("SQL UPDATE: Group {Id} -> sequence {Seq}, rows affected: {Rows}", 
                        item.Id, exactSequence, rowsAffected);
                }
            });

            // Verify final state
            var ids = items.Select(i => i.Id).ToList();
            var finalGroups = await _context.LinkGroups
                .Where(g => ids.Contains(g.Id) && g.UserId == userId)
                .Select(g => new { g.Id, g.Sequence, g.Name })
                .OrderBy(g => g.Sequence)
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("FINAL GROUP STATE: {Groups}", finalGroups);

            return OkEnvelope(new 
            { 
                Message = "Groups resequenced exactly as specified", 
                FinalState = finalGroups 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resequencing groups for user {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while resequencing groups");
        }
    }

    private async Task<int?> GetActiveTierIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.UserTiers
            .AsNoTracking()
            .Where(ut => ut.UserId == userId &&
                         ut.IsActive &&
                         ut.ActiveFrom <= now &&
                         (ut.ActiveUntil == null || ut.ActiveUntil >= now))
            .OrderByDescending(ut => ut.ActiveFrom)
            .Select(ut => (int?)ut.TierId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
