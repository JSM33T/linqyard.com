using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Linqyard.Contracts.Interfaces;

public interface ILinkRepository
{
    /// <summary>Return groups + links for the authenticated user.</summary>
    Task<LinksGroupedResponse> GetLinksForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Return groups + links for a public username; null if user not found.</summary>
    Task<LinksGroupedResponse?> GetLinksByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>Create a link for the user, enforcing tier limits and basic validations.</summary>
    Task<LinkSummary> CreateLinkAsync(
        Guid userId,
        CreateLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially update a link. Enforces owner/admin permissions and validates Group ownership.
    /// Returns null if link not found, throws ForbiddenAccessException if not permitted.
    /// </summary>
    Task<LinkSummary?> EditLinkAsync(
        Guid editorUserId,
        Guid linkId,
        EditLinkRequest request,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resequence/move links exactly as specified (id, groupId, sequence). Returns the final states.
    /// Throws ForbiddenAccessException if any item does not belong to user.
    /// </summary>
    Task<IReadOnlyList<LinkSequenceState>> ResequenceAsync(
        Guid userId,
        IReadOnlyList<ResequenceItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a link with owner/admin enforcement.
    /// Returns: NotFound, Forbidden, or Deleted.
    /// </summary>
    Task<DeleteLinkResult> DeleteLinkAsync(
        Guid requesterUserId,
        Guid linkId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}