using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class AdminRolePolicy
{
    public static void EnsureCanPromoteToAdmin(IReadOnlyCollection<string> targetRoles)
    {
        Contracts.Require(targetRoles != null && targetRoles.Contains(Role.Constants.Lector),
            "Only teachers (lectors) can be promoted to administrator.");
        Contracts.Require(!targetRoles.Contains(Role.Constants.Admin),
            "User is already an administrator.");
    }

    public static void EnsureCanDemoteFromAdmin(IReadOnlyCollection<string> targetRoles, int currentAdminCount)
    {
        Contracts.Require(targetRoles != null && targetRoles.Contains(Role.Constants.Admin),
            "User is not an administrator.");
        Contracts.Require(currentAdminCount > 1,
            "There must always be at least one administrator in the system.");
    }
}
