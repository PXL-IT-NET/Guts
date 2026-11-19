using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class UserDeletionPolicy
{
    public static bool CanBeDeleted(
        User targetUser,
        int currentUserId,
        IReadOnlyCollection<string> targetRoles)
    {
        return targetUser is not null &&
               targetUser.Id != currentUserId &&
               (targetRoles == null || !targetRoles.Contains(Role.Constants.Admin));
    }

    public static void EnsureCanBeDeleted(
        User targetUser,
        int currentUserId,
        IReadOnlyCollection<string> targetRoles)
    {
        Contracts.Require(targetUser is not null, "Target user cannot be null.");
        Contracts.Require(targetUser.Id != currentUserId, "You cannot delete your own account.");
        Contracts.Require(targetRoles == null || !targetRoles.Contains(Role.Constants.Admin),
            "Administrator accounts cannot be deleted.");
    }
}
