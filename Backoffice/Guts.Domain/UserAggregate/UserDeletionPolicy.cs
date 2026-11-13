using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class UserDeletionPolicy
{
    public static void EnsureCanBeDeleted(
        User targetUser,
        int currentUserId,
        IReadOnlyCollection<string> targetRoles,
        UserCourseLinks links)
    {
        Contracts.Require(targetUser is not null, "Target user cannot be null.");
        Contracts.Require(links is not null, "User course links cannot be null.");
        Contracts.Require(targetUser.Id != currentUserId, "You cannot delete your own account.");
        Contracts.Require(targetRoles == null || !targetRoles.Contains(Role.Constants.Admin),
            "Administrator accounts cannot be deleted.");
        Contracts.Require(!links.HasAnyLinks,
            "User cannot be deleted because they are still linked to course data.");
    }
}
