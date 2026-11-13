using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Application.Services;

internal class AdminUserService(IUserRepository userRepository) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserDto>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool OrphanedOnly,
        int currentUserId)
    {
        IReadOnlyList<UserWithRolesAndLinks> results =
            await userRepository.SearchUsersAsync(search, roleFilter, OrphanedOnly);
        int adminCount = await userRepository.CountAdminsAsync();

        return results.Select(entry =>
        {
            bool isAdmin = entry.Roles.Contains(Role.Constants.Admin);
            bool isLector = entry.Roles.Contains(Role.Constants.Lector);
            bool isLinked = entry.CourseLinks.HasAnyLinks;
            return new AdminUserDto
            {
                Id = entry.User.Id,
                Email = entry.User.Email,
                FirstName = entry.User.FirstName,
                LastName = entry.User.LastName,
                Roles = entry.Roles,
                IsLinkedToCourse = isLinked,
                CanDelete = !isAdmin && entry.User.Id != currentUserId && !isLinked,
                CanImpersonate = !isAdmin,
                CanPromoteToAdmin = isLector && !isAdmin,
                CanDemoteFromAdmin = isAdmin && adminCount > 1
            };
        }).ToList();
    }

    public async Task DeleteUserAsync(int targetUserId, int currentUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);
        UserCourseLinks links = await userRepository.GetCourseLinksAsync(targetUserId);

        UserDeletionPolicy.EnsureCanBeDeleted(targetUser, currentUserId, targetRoles, links);

        await userRepository.DeleteAsync(targetUser);
    }

    public async Task<int> DeleteAllOrphanedUsersAsync(int currentUserId)
    {
        IReadOnlyList<UserWithRolesAndLinks> candidates = await userRepository.GetOrphanedNonAdminUsersAsync();
        int deletedCount = 0;

        foreach (UserWithRolesAndLinks candidate in candidates)
        {
            if (candidate.User.Id == currentUserId)
            {
                continue;
            }

            UserDeletionPolicy.EnsureCanBeDeleted(candidate.User, currentUserId, candidate.Roles, candidate.CourseLinks);
            await userRepository.DeleteAsync(candidate.User);
            deletedCount++;
        }

        return deletedCount;
    }

    public async Task PromoteToAdminAsync(int targetUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);

        AdminRolePolicy.EnsureCanPromoteToAdmin(targetRoles);

        await userRepository.AddToRoleAsync(targetUser, Role.Constants.Admin);
    }

    public async Task DemoteFromAdminAsync(int targetUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);
        int currentAdminCount = await userRepository.CountAdminsAsync();

        AdminRolePolicy.EnsureCanDemoteFromAdmin(targetRoles, currentAdminCount);

        await userRepository.RemoveFromRoleAsync(targetUser, Role.Constants.Admin);
    }
}
