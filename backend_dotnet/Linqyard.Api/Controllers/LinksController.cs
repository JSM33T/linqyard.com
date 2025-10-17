using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Linqyard.Entities;
using Linqyard.Entities.Enums;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Linqyard.Contracts;
using System.Text.Json;
using Linqyard.Data;

namespace Linqyard.Api.Controllers;

// DTO for resequence payload
public record ReSequenceItemRequest(Guid Id, Guid? GroupId, int Sequence);
[Route("link")]
[ApiController]
[Authorize]
public class LinksController : BaseApiController
{
    private readonly LinqyardDbContext _context;
    private readonly ILogger<LinksController> _logger;

    public LinksController(ILogger<LinksController> logger, LinqyardDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get links grouped by their group. Ungrouped links are returned in the Ungrouped field.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(ApiResponse<LinksGroupedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLinks(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            // Load groups owned by this user and their links
            var groups = await _context.LinkGroups
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Sequence)
                .ToListAsync(cancellationToken);

            var links = await _context.Links
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.Sequence)
                .ToListAsync(cancellationToken);

            // Map grouped
            var groupResponses = groups.Select(g => new LinkGroupResponse(
                Id: g.Id,
                Name: g.Name,
                Description: g.Description,
                Sequence: g.Sequence,
                IsActive: g.IsActive,
                Links: links.Where(l => l.GroupId == g.Id).OrderBy(l => l.Sequence).Select(l => new LinkSummary(
                    Id: l.Id,
                    Name: l.Name,
                    Url: l.Url,
                    Description: l.Description,
                    IsActive: l.IsActive,
                    Sequence: l.Sequence,
                    GroupId: l.GroupId,
                    CreatedAt: l.CreatedAt,
                    UpdatedAt: l.UpdatedAt
                )).ToList()
            )).ToList();

            // Ungrouped links
            var ungrouped = new LinkGroupResponse(
                Id: Guid.Empty,
                Name: "Ungrouped",
                Description: null,
                Sequence: 0,
                IsActive: true,
                Links: links.Where(l => l.GroupId == null).OrderBy(l => l.Sequence).Select(l => new LinkSummary(
                    Id: l.Id,
                    Name: l.Name,
                    Url: l.Url,
                    Description: l.Description,
                    IsActive: l.IsActive,
                    Sequence: l.Sequence,
                    GroupId: l.GroupId,
                    CreatedAt: l.CreatedAt,
                    UpdatedAt: l.UpdatedAt
                )).ToList()
            );

            var response = new LinksGroupedResponse(Groups: groupResponses, Ungrouped: ungrouped);

            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links for user {UserId}", UserId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving links");
        }
    }

    /// <summary>
    /// Get links grouped by group for a public username (anonymous access).
    /// </summary>
    [HttpGet("user/{username}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LinksGroupedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinksByUsername(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize username to lowercase for case-insensitive lookup
            var usernameNorm = (username ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(usernameNorm)) return NotFoundProblem("User not found");

            // Resolve user by normalized username (case-insensitive)
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == usernameNorm, cancellationToken);
            if (user == null) return NotFoundProblem("User not found");

            var userId = user.Id;

            // Load groups owned by this user and their links
            var groups = await _context.LinkGroups
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Sequence)
                .ToListAsync(cancellationToken);

            var links = await _context.Links
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.Sequence)
                .ToListAsync(cancellationToken);

            var groupResponses = groups.Select(g => new LinkGroupResponse(
                Id: g.Id,
                Name: g.Name,
                Description: g.Description,
                Sequence: g.Sequence,
                IsActive: g.IsActive,
                Links: links.Where(l => l.GroupId == g.Id).OrderBy(l => l.Sequence).Select(l => new LinkSummary(
                    Id: l.Id,
                    Name: l.Name,
                    Url: l.Url,
                    Description: l.Description,
                    IsActive: l.IsActive,
                    Sequence: l.Sequence,
                    GroupId: l.GroupId,
                    CreatedAt: l.CreatedAt,
                    UpdatedAt: l.UpdatedAt
                )).ToList()
            )).ToList();

            var ungrouped = new LinkGroupResponse(
                Id: Guid.Empty,
                Name: "Ungrouped",
                Description: null,
                Sequence: 0,
                IsActive: true,
                Links: links.Where(l => l.GroupId == null).OrderBy(l => l.Sequence).Select(l => new LinkSummary(
                    Id: l.Id,
                    Name: l.Name,
                    Url: l.Url,
                    Description: l.Description,
                    IsActive: l.IsActive,
                    Sequence: l.Sequence,
                    GroupId: l.GroupId,
                    CreatedAt: l.CreatedAt,
                    UpdatedAt: l.UpdatedAt
                )).ToList()
            );

            var response = new LinksGroupedResponse(Groups: groupResponses, Ungrouped: ungrouped);

            return OkEnvelope(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while retrieving links");
        }
    }

    /// <summary>
    /// Create a new link
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(ApiResponse<LinkSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating link for user {UserId} with CorrelationId {CorrelationId}", UserId, CorrelationId);

        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
                return BadRequestProblem("Name and Url are required");

            // Check tier-based limits for free tier users
            var activeTierId = await GetActiveTierIdAsync(userId, cancellationToken) ?? (int)TierType.Free;

            if (activeTierId == (int)TierType.Free)
            {
                var existingLinksCount = await _context.Links
                    .CountAsync(l => l.UserId == userId, cancellationToken);

                if (existingLinksCount >= 12)
                {
                    return BadRequestProblem("Free tier users can create a maximum of 12 links. Please upgrade to create more links.");
                }
            }

            // Optional: validate URL format
            if (!Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
                return BadRequestProblem("Url is not a valid absolute URL");

            // If GroupId provided, ensure group exists and is owned by the user
            if (request.GroupId.HasValue)
            {
                // treat Guid.Empty as explicit ungroup
                if (request.GroupId.Value == Guid.Empty)
                {
                    request = request with { GroupId = null };
                }
                else
                {
                    var group = await _context.LinkGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId.Value, cancellationToken);
                    if (group == null || group.UserId != userId)
                        return BadRequestProblem("Specified group does not exist or does not belong to you");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var link = new Link
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Url = request.Url.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                UserId = userId,
                GroupId = request.GroupId,
                Sequence = request.Sequence ?? 0,
                IsActive = request.IsActive ?? true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Links.Add(link);
            await _context.SaveChangesAsync(cancellationToken);

            var summary = new LinkSummary(
                Id: link.Id,
                Name: link.Name,
                Url: link.Url,
                Description: link.Description,
                IsActive: link.IsActive,
                Sequence: link.Sequence,
                GroupId: link.GroupId,
                CreatedAt: link.CreatedAt,
                UpdatedAt: link.UpdatedAt
            );

            return OkEnvelope(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating link for user {UserId}", UserId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while creating the link");
        }
    }

    /// <summary>
    /// Edit a link by id (only owner or admin). POST-based edit to follow GET/POST-only policy.
    /// </summary>
    [HttpPost("{id:guid}/edit")]
    [ProducesResponseType(typeof(ApiResponse<LinkSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditLink(Guid id, [FromBody] JsonElement requestJson, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editing link {LinkId} by user {UserId}", id, UserId);

        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");
            var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (link == null) return NotFoundProblem("Link not found");

            var isOwner = link.UserId == userId;
            var isAdmin = User.IsInRole("admin");
            if (!isOwner && !isAdmin) return ForbiddenProblem("You do not have permission to edit this link");

            // Deserialize into the request DTO for convenience, but keep the raw JSON to detect explicit nulls
            var request = JsonSerializer.Deserialize<CreateLinkRequest>(requestJson.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (request == null)
                return BadRequestProblem("Invalid request payload");

            if (!string.IsNullOrWhiteSpace(request.Name)) link.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                if (!Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
                    return BadRequestProblem("Url is not a valid absolute URL");
                link.Url = request.Url.Trim();
            }

            if (request.Description != null) link.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            // Handle group updates specially: we want to allow an explicit null to ungroup.
            // Check whether the incoming JSON contains the "groupId" property.
            if (requestJson.TryGetProperty("groupId", out var groupProp))
            {
                if (groupProp.ValueKind == JsonValueKind.Null)
                {
                    // explicit null -> ungroup
                    link.GroupId = null;
                }
                else if (groupProp.ValueKind == JsonValueKind.String)
                {
                    var groupStr = groupProp.GetString();
                    if (string.IsNullOrWhiteSpace(groupStr))
                    {
                        link.GroupId = null;
                    }
                    else if (Guid.TryParse(groupStr, out var parsedGroupId))
                    {
                        if (parsedGroupId == Guid.Empty)
                        {
                            // treat empty guid as ungroup
                            link.GroupId = null;
                        }
                        else
                        {
                            var group = await _context.LinkGroups.FirstOrDefaultAsync(g => g.Id == parsedGroupId, cancellationToken);
                            if (group == null || (group.UserId != userId && !isAdmin))
                                return BadRequestProblem("Specified group does not exist or does not belong to you");

                            link.GroupId = parsedGroupId;
                        }
                    }
                    else
                    {
                        return BadRequestProblem("Specified group id is not a valid GUID");
                    }
                }
                else
                {
                    // If it's another JSON type (e.g., object/number), reject it
                    return BadRequestProblem("Invalid groupId value");
                }
            }

            if (request.Sequence.HasValue) link.Sequence = request.Sequence.Value;

            if (request.IsActive.HasValue) link.IsActive = request.IsActive.Value;
            link.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var summary = new LinkSummary(
                Id: link.Id,
                Name: link.Name,
                Url: link.Url,
                Description: link.Description,
                IsActive: link.IsActive,
                Sequence: link.Sequence,
                GroupId: link.GroupId,
                CreatedAt: link.CreatedAt,
                UpdatedAt: link.UpdatedAt
            );

            return OkEnvelope(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing link {LinkId} by user {UserId}", id, UserId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while editing the link");
        }
    }

    /// <summary>
    /// Delete a link by id (only owner or admin) - POST-based delete to follow GET/POST-only policy
    /// </summary>
    /// <summary>
    /// Resequence links (and optionally move them between groups). POST-only.
    /// Body: [{ "id": "...", "groupId": "...|null", "sequence": 0 }, ...]
    /// </summary>
    [HttpPost("resequence")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Resequence([FromBody] List<ReSequenceItemRequest> items, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RESEQUENCE START: {Count} items", items?.Count ?? 0);

        if (!Guid.TryParse(UserId, out var userId))
            return UnauthorizedProblem("Invalid user context");

        if (items == null || items.Count == 0)
            return BadRequestProblem("No items provided");

        // Process each item exactly as sent - use EXACT sequence you specify
        foreach (var item in items)
        {
            var groupId = item.GroupId;
            var exactSequence = item.Sequence; // Use EXACTLY what you send
            
            _logger.LogInformation("Setting link {Id} to EXACT sequence {Seq} in group {GroupId}", 
                item.Id, exactSequence, groupId?.ToString() ?? "UNGROUPED");
                
            // Direct SQL update - set sequence AND group
            int rowsAffected;
            if (groupId.HasValue)
            {
                rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Links\" SET \"Sequence\" = {0}, \"GroupId\" = {1}, \"UpdatedAt\" = {2} WHERE \"Id\" = {3} AND \"UserId\" = {4}",
                    exactSequence, groupId.Value, DateTimeOffset.UtcNow, item.Id, userId);
            }
            else
            {
                rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Links\" SET \"Sequence\" = {0}, \"GroupId\" = NULL, \"UpdatedAt\" = {1} WHERE \"Id\" = {2} AND \"UserId\" = {3}",
                    exactSequence, DateTimeOffset.UtcNow, item.Id, userId);
            }
            
            _logger.LogInformation("SQL UPDATE: Link {Id} -> sequence {Seq}, rows affected: {Rows}", 
                item.Id, exactSequence, rowsAffected);
        }

        // Verify final state
        var ids = items.Select(i => i.Id).ToList();
        var finalLinks = await _context.Links
            .Where(l => ids.Contains(l.Id))
            .Select(l => new { l.Id, l.Sequence, l.GroupId })
            .OrderBy(l => l.GroupId).ThenBy(l => l.Sequence)
            .ToListAsync(cancellationToken);
            
        _logger.LogInformation("FINAL DATABASE STATE:");
        foreach (var link in finalLinks)
        {
            _logger.LogInformation("Link {Id}: sequence={Seq}, group={GroupId}", 
                link.Id, link.Sequence, link.GroupId?.ToString() ?? "UNGROUPED");
        }

        return OkEnvelope(new { 
            Message = "Resequenced exactly as specified", 
            FinalState = finalLinks 
        });
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLink(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting link {LinkId} by user {UserId}", id, UserId);

        try
        {
            if (!Guid.TryParse(UserId, out var userId))
                return UnauthorizedProblem("Invalid user context");

            var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (link == null)
                return NotFoundProblem("Link not found");

            // Allow deletion if owner or admin
            var isOwner = link.UserId == userId;
            var isAdmin = User.IsInRole("admin");

            if (!isOwner && !isAdmin)
                return ForbiddenProblem("You do not have permission to delete this link");

            _context.Links.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);

            return OkEnvelope(new { Message = "Link deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting link {LinkId} by user {UserId}", id, UserId);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "An error occurred while deleting the link");
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
