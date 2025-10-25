using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Provides persistence operations for storing and retrieving links owned by users.
    /// </summary>
    public interface ILinkRepository
    {
        /// <summary>
        /// Returns all link groups and their links for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the owning user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>Grouped links for the authenticated user.</returns>
        Task<LinksGroupedResponse> GetLinksForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns public link groups for the specified username.
        /// </summary>
        /// <param name="username">Public username of the profile owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The grouped links if the user exists; otherwise, <c>null</c>.</returns>
        Task<LinksGroupedResponse?> GetLinksByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a link for the user while enforcing tier limits and validation rules.
        /// </summary>
        /// <param name="userId">Unique identifier of the link owner.</param>
        /// <param name="request">Payload describing the new link.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created link summary.</returns>
        Task<LinkSummary> CreateLinkAsync(
            Guid userId,
            CreateLinkRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing link after enforcing owner or admin permissions.
        /// </summary>
        /// <param name="editorUserId">User performing the edit.</param>
        /// <param name="linkId">Identifier of the link to edit.</param>
        /// <param name="request">Payload describing updates to apply.</param>
        /// <param name="isAdmin">Indicates whether the requester is an administrator.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link summary, or <c>null</c> if the link does not exist.</returns>
        Task<LinkSummary?> EditLinkAsync(
            Guid editorUserId,
            Guid linkId,
            EditLinkRequest request,
            bool isAdmin,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resequences and/or moves links between groups for the specified user.
        /// </summary>
        /// <param name="userId">Owner of the links being resequenced.</param>
        /// <param name="items">Instructions describing the new sequence order.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The final sequence states for the affected links.</returns>
        Task<IReadOnlyList<LinkSequenceState>> ResequenceAsync(
            Guid userId,
            IReadOnlyList<ResequenceItem> items,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a link with owner or admin enforcement.
        /// </summary>
        /// <param name="requesterUserId">User initiating the deletion.</param>
        /// <param name="linkId">Identifier of the link to remove.</param>
        /// <param name="isAdmin">Indicates whether the requester is an administrator.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The outcome of the delete attempt.</returns>
        Task<DeleteLinkResult> DeleteLinkAsync(
            Guid requesterUserId,
            Guid linkId,
            bool isAdmin,
            CancellationToken cancellationToken = default);
    }
}
