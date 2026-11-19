using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Security;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Application.Services;

internal class ImpersonationService(
    IUserRepository userRepository,
    ITokenAccessPassFactory tokenAccessPassFactory)
    : IImpersonationService
{
    public async Task<PagedResult<ImpersonatableUserDto>> SearchImpersonatableUsersAsync(
        string search,
        IReadOnlyCollection<string> callerRoles,
        int page = 1,
        int? pageSize = null)
    {
        var (roleFilter, excludedRoles) = ImpersonationPolicy.GetSearchFilterForCaller(callerRoles);

        PagedResult<UserWithRolesAndLinks> results =
            await userRepository.SearchUsersAsync(search, roleFilter, orphanedOnly: false, excludedRoles, page, pageSize);

        List<ImpersonatableUserDto> items = results.Items
            .Where(entry => ImpersonationPolicy.CanCallerImpersonateTarget(callerRoles, entry.Roles))
            .Select(entry => new ImpersonatableUserDto
            {
                Id = entry.User.Id,
                Email = entry.User.Email,
                FirstName = entry.User.FirstName,
                LastName = entry.User.LastName,
                Roles = entry.Roles
            })
            .ToList();

        return new PagedResult<ImpersonatableUserDto>
        {
            Items = items,
            TotalCount = results.TotalCount,
            Page = results.Page,
            PageSize = results.PageSize
        };
    }

    public async Task<TokenAccessPass> ImpersonateAsync(
        int callerUserId,
        IReadOnlyCollection<string> callerRoles,
        bool isAlreadyImpersonating,
        int targetUserId)
    {
        User targetUser = await userRepository.GetByIdAsync(targetUserId) ?? throw new DataNotFoundException();
        IReadOnlyList<string> targetRoles = await userRepository.GetRolesAsync(targetUser);

        ImpersonationPolicy.EnsureCanImpersonate(callerRoles, isAlreadyImpersonating, targetUser, targetRoles);

        IList<Claim> targetClaims = await userRepository.GetClaimsAsync(targetUser);
        return tokenAccessPassFactory.Create(targetUser, targetClaims, targetRoles.ToList(), impersonatorId: callerUserId);
    }
}
