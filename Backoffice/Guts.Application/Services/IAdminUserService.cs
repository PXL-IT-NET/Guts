using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;

namespace Guts.Application.Services;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool orphanedOnly,
        int currentUserId,
        int page = 1,
        int? pageSize = null);

    Task DeleteUserAsync(int targetUserId, int currentUserId);
    Task<int> DeleteUsersAsync(IReadOnlyCollection<int> targetUserIds, int currentUserId);
    Task PromoteToAdminAsync(int targetUserId);
    Task DemoteFromAdminAsync(int targetUserId);
}
