using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;

namespace Guts.Application.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> SearchUsersAsync(string search, string roleFilter, bool OrphanedOnly, int currentUserId);
    Task DeleteUserAsync(int targetUserId, int currentUserId);
    Task<int> DeleteAllOrphanedUsersAsync(int currentUserId);
    Task PromoteToAdminAsync(int targetUserId);
    Task DemoteFromAdminAsync(int targetUserId);
}
