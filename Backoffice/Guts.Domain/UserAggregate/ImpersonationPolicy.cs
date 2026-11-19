using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class ImpersonationPolicy
{
    public static bool CanBeImpersonated(IReadOnlyCollection<string> targetRoles)
    {
        return targetRoles == null || !targetRoles.Contains(Role.Constants.Admin);
    }

    public static bool CanCallerImpersonateTarget(
        IReadOnlyCollection<string> callerRoles,
        IReadOnlyCollection<string> targetRoles)
    {
        if (!CanBeImpersonated(targetRoles))
        {
            return false;
        }

        bool isAdmin = callerRoles != null && callerRoles.Contains(Role.Constants.Admin);
        if (isAdmin)
        {
            return true;
        }

        bool isLector = callerRoles != null && callerRoles.Contains(Role.Constants.Lector);
        if (!isLector)
        {
            return false;
        }

        bool targetIsStudent = targetRoles != null && targetRoles.Contains(Role.Constants.Student);
        bool targetIsLector = targetRoles != null && targetRoles.Contains(Role.Constants.Lector);
        return targetIsStudent && !targetIsLector;
    }

    public static (string RoleFilter, IReadOnlyCollection<string> ExcludedRoles) GetSearchFilterForCaller(
        IReadOnlyCollection<string> callerRoles)
    {
        bool isAdmin = callerRoles != null && callerRoles.Contains(Role.Constants.Admin);
        string roleFilter = isAdmin ? null : Role.Constants.Student;
        IReadOnlyCollection<string> excludedRoles = isAdmin
            ? [Role.Constants.Admin]
            : [Role.Constants.Admin, Role.Constants.Lector];

        return (roleFilter, excludedRoles);
    }

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
