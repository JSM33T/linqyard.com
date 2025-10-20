using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public interface IGroupService
{
    Task<IReadOnlyList<LinkGroupResponse>> GetGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkGroupResponse>> GetGroupsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<LinkGroupResponse> CreateGroupAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<LinkGroupResponse> UpdateGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<LinkGroupResponse> UpdateGroupStatusAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task DeleteGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<GroupResequenceResult> ResequenceGroupsAsync(
        Guid userId,
        IReadOnlyList<GroupResequenceItemRequest> items,
        CancellationToken cancellationToken = default);
}

