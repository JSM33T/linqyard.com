using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public interface IUserRepository
{
    Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);
    Task<UserPublicResponse?> GetPublicByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserBasicResponse?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminUserListItemResponse> Users, long Total)> SearchAdminUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailsResponse?> GetAdminUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailsResponse?> UpdateAdminUserAsync(
        Guid userId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default);
}
