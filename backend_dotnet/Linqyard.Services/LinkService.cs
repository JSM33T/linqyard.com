using Linqyard.Contracts.Exceptions;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Services;

public sealed class LinkService : ILinkService
{
    private readonly ILinkRepository _linkRepository;

    public LinkService(ILinkRepository linkRepository)
    {
        _linkRepository = linkRepository ?? throw new ArgumentNullException(nameof(linkRepository));
    }

    public Task<LinksGroupedResponse> GetLinksAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _linkRepository.GetLinksForUserAsync(userId, cancellationToken);

    public async Task<LinksGroupedResponse> GetLinksByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var result = await _linkRepository.GetLinksByUsernameAsync(username, cancellationToken);
        return result ?? throw new LinkNotFoundException("User not found.");
    }

    public Task<LinkSummary> CreateLinkAsync(
        Guid userId,
        CreateLinkRequest request,
        CancellationToken cancellationToken = default) =>
        _linkRepository.CreateLinkAsync(userId, request, cancellationToken);

    public async Task<LinkSummary> UpdateLinkAsync(
        Guid linkId,
        Guid editorUserId,
        bool isAdmin,
        EditLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.EditLinkAsync(editorUserId, linkId, request, isAdmin, cancellationToken);
        return link ?? throw new LinkNotFoundException("Link not found.");
    }

    public Task<IReadOnlyList<LinkSequenceState>> ResequenceLinksAsync(
        Guid userId,
        IReadOnlyList<ResequenceItem> items,
        CancellationToken cancellationToken = default) =>
        _linkRepository.ResequenceAsync(userId, items, cancellationToken);

    public async Task DeleteLinkAsync(
        Guid linkId,
        Guid requesterUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var result = await _linkRepository.DeleteLinkAsync(requesterUserId, linkId, isAdmin, cancellationToken);

        switch (result)
        {
            case DeleteLinkResult.NotFound:
                throw new LinkNotFoundException("Link not found.");
            case DeleteLinkResult.Forbidden:
                throw new LinkForbiddenException("You do not have permission to delete this link.");
            case DeleteLinkResult.Deleted:
                return;
            default:
                throw new LinkServiceException("Unexpected delete result.");
        }
    }
}

