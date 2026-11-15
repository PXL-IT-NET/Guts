using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Security;

namespace Guts.Application.Services;

public interface IImpersonationService
{
    Task<PagedResult<ImpersonatableUserDto>> SearchImpersonatableUsersAsync(
        string search,
        IReadOnlyCollection<string> callerRoles,
        int page = 1,
        int? pageSize = null);

    Task<TokenAccessPass> ImpersonateAsync(
        int callerUserId,
        IReadOnlyCollection<string> callerRoles,
        bool isAlreadyImpersonating,
        int targetUserId);
}
