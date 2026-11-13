using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class ImpersonationPolicy
{
    public static void EnsureCanImpersonate(
        IReadOnlyCollection<string> callerRoles,
        bool isAlreadyImpersonating,
        User targetUser,
        IReadOnlyCollection<string> targetRoles)
    {
        Contracts.Require(!isAlreadyImpersonating, "Nested impersonation is not allowed.");
        Contracts.Require(targetUser is not null, "Target user cannot be null.");

        bool isAdmin = callerRoles != null && callerRoles.Contains(Role.Constants.Admin);
        bool isLector = callerRoles != null && callerRoles.Contains(Role.Constants.Lector);
        Contracts.Require(isAdmin || isLector, "Only administrators or lectors can impersonate users.");

        bool targetIsAdmin = targetRoles != null && targetRoles.Contains(Role.Constants.Admin);
        Contracts.Require(!targetIsAdmin, "Administrator accounts cannot be impersonated.");

        if (!isAdmin)
        {
            bool targetIsStudent = targetRoles != null && targetRoles.Contains(Role.Constants.Student);
            bool targetIsLector = targetRoles != null && targetRoles.Contains(Role.Constants.Lector);
            Contracts.Require(targetIsStudent && !targetIsLector,
                "Lectors can only impersonate student accounts.");
        }
    }
}
