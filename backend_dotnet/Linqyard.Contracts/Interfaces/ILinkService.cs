using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Provides business logic for creating, updating, ordering, and deleting links.
    /// </summary>
    public interface ILinkService
    {
        /// <summary>
        /// Returns the links for the specified user, grouped by their containers.
        /// </summary>
        /// <param name="userId">Unique identifier of the link owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The grouped link response.</returns>
        Task<LinksGroupedResponse> GetLinksAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the public links for the specified username.
        /// </summary>
        /// <param name="username">Public username of the profile owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The grouped link response.</returns>
        Task<LinksGroupedResponse> GetLinksByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new link for the user.
        /// </summary>
        /// <param name="userId">Unique identifier of the link owner.</param>
        /// <param name="request">Payload describing the link to create.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created link summary.</returns>
        Task<LinkSummary> CreateLinkAsync(
            Guid userId,
            CreateLinkRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing link on behalf of the specified editor.
        /// </summary>
        /// <param name="linkId">Identifier of the link being updated.</param>
        /// <param name="editorUserId">Identifier of the user performing the edit.</param>
        /// <param name="isAdmin">Whether the editor is an administrator.</param>
        /// <param name="request">Payload describing the fields to update.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link summary.</returns>
        Task<LinkSummary> UpdateLinkAsync(
            Guid linkId,
            Guid editorUserId,
            bool isAdmin,
            EditLinkRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resequences a user's links to match the requested order.
        /// </summary>
        /// <param name="userId">Identifier of the link owner.</param>
        /// <param name="items">Sequence instructions for each link.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The final sequence states.</returns>
        Task<IReadOnlyList<LinkSequenceState>> ResequenceLinksAsync(
            Guid userId,
            IReadOnlyList<ResequenceItem> items,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a link after enforcing ownership or admin permissions.
        /// </summary>
        /// <param name="linkId">Identifier of the link to delete.</param>
        /// <param name="requesterUserId">Identifier of the user performing the delete.</param>
        /// <param name="isAdmin">Whether the requester is an administrator.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task DeleteLinkAsync(
            Guid linkId,
            Guid requesterUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default);
    }
}

