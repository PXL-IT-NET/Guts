using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Security;

namespace Guts.Application.Services;

public interface IImpersonationService
{
    Task<IReadOnlyList<ImpersonatableUserDto>> SearchImpersonatableUsersAsync(
        string search,
        IReadOnlyCollection<string> callerRoles);

    Task<TokenAccessPass> ImpersonateAsync(
        int callerUserId,
        IReadOnlyCollection<string> callerRoles,
        bool isAlreadyImpersonating,
        int targetUserId);
}
