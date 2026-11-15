using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Application.Services;

internal class AdminUserService(IUserRepository userRepository) : IAdminUserService
{
    public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool orphanedOnly,
        bool lockedOnly,
        int currentUserId,
        int page = 1,
        int? pageSize = null)
    {
        PagedResult<UserWithRolesAndLinks> results =
            await userRepository.SearchUsersAsync(search, roleFilter, orphanedOnly, lockedOnly, page, pageSize);
        int adminCount = await userRepository.CountAdminsAsync();
        DateTime utcNow = DateTime.UtcNow;

        List<AdminUserDto> items = results.Items.Select(entry =>
        {
            bool isAdmin = entry.Roles.Contains(Role.Constants.Admin);
            bool isLector = entry.Roles.Contains(Role.Constants.Lector);
            bool isLinked = entry.CourseLinks.HasAnyLinks;
            bool isLockedLector = LectorVerificationPolicy.IsLocked(entry.User, entry.Roles, utcNow);
            return new AdminUserDto
            {
                Id = entry.User.Id,
                Email = entry.User.Email,
                FirstName = entry.User.FirstName,
                LastName = entry.User.LastName,
                Roles = entry.Roles,
                IsLinkedToCourse = isLinked,
                LinkedCourses = entry.LinkedCourses ?? [],
                CanDelete = !isAdmin && entry.User.Id != currentUserId,
                CanImpersonate = !isAdmin,
                CanPromoteToAdmin = isLector && !isAdmin,
                CanDemoteFromAdmin = isAdmin && adminCount > 1,
                LastVerifiedOnUtc = entry.User.LastVerifiedOnUtc,
                IsLockedLector = isLockedLector,
                CanUnlockLector = isLockedLector
            };
        }).ToList();

        return new PagedResult<AdminUserDto>
        {
            Items = items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };
    }

    public async Task DeleteUserAsync(int targetUserId, int currentUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);

        UserDeletionPolicy.EnsureCanBeDeleted(targetUser, currentUserId, targetRoles);

        await userRepository.DeleteAsync(targetUser);
    }

    public async Task<int> DeleteUsersAsync(IReadOnlyCollection<int> targetUserIds, int currentUserId)
    {
        List<int> distinctIds = (targetUserIds ?? []).Where(id => id > 0).Distinct().ToList();
        Contracts.Require(distinctIds.Count > 0, "At least one user must be selected.");

        IReadOnlyList<UserWithRolesAndLinks> candidates = await userRepository.GetWithRolesAndLinksByIdsAsync(distinctIds);
        if (candidates.Count != distinctIds.Count)
        {
            throw new DataNotFoundException();
        }

        foreach (UserWithRolesAndLinks candidate in candidates)
        {
            UserDeletionPolicy.EnsureCanBeDeleted(candidate.User, currentUserId, candidate.Roles);
        }

        await userRepository.DeleteRangeAsync(candidates.Select(c => c.User).ToList());
        return candidates.Count;
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

    public async Task UnlockLectorAsync(int targetUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);
        DateTime utcNow = DateTime.UtcNow;

        LectorVerificationPolicy.EnsureCanUnlock(targetUser, targetRoles, utcNow);

        targetUser.MarkEmailVerified(utcNow);
        await userRepository.UpdateAsync(targetUser);
    }
}
