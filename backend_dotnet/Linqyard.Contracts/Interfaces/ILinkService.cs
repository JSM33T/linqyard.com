using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public interface ILinkService
{
    Task<LinksGroupedResponse> GetLinksAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<LinksGroupedResponse> GetLinksByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<LinkSummary> CreateLinkAsync(
        Guid userId,
        CreateLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<LinkSummary> UpdateLinkAsync(
        Guid linkId,
        Guid editorUserId,
        bool isAdmin,
        EditLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkSequenceState>> ResequenceLinksAsync(
        Guid userId,
        IReadOnlyList<ResequenceItem> items,
        CancellationToken cancellationToken = default);

    Task DeleteLinkAsync(
        Guid linkId,
        Guid requesterUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}

