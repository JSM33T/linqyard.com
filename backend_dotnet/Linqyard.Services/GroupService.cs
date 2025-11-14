using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Services;

public sealed class GroupService(IGroupRepository groupRepository) : IGroupService
{
    private readonly IGroupRepository _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));

    public Task<IReadOnlyList<LinkGroupResponse>> GetGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _groupRepository.GetGroupsAsync(userId, cancellationToken);

    public Task<IReadOnlyList<LinkGroupResponse>> GetGroupsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        _groupRepository.GetGroupsByUsernameAsync(username, cancellationToken);

    public Task<LinkGroupResponse> CreateGroupAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken cancellationToken = default) =>
        _groupRepository.CreateGroupAsync(userId, request, cancellationToken);

    public Task<LinkGroupResponse> UpdateGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default) =>
        _groupRepository.UpdateGroupAsync(groupId, userId, isAdmin, request, cancellationToken);

    public Task<LinkGroupResponse> UpdateGroupStatusAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        bool isActive,
        CancellationToken cancellationToken = default) =>
        _groupRepository.UpdateGroupStatusAsync(groupId, userId, isAdmin, isActive, cancellationToken);

    public Task DeleteGroupAsync(
        Guid groupId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default) =>
        _groupRepository.DeleteGroupAsync(groupId, userId, isAdmin, cancellationToken);

    public Task<GroupResequenceResult> ResequenceGroupsAsync(
        Guid userId,
        IReadOnlyList<GroupResequenceItemRequest> items,
        CancellationToken cancellationToken = default) =>
        _groupRepository.ResequenceGroupsAsync(userId, items, cancellationToken);
}

