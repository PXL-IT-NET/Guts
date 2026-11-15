using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.RoleAggregate;

namespace Guts.Domain.UserAggregate;

public static class LectorVerificationPolicy
{
    public static DateTime GetVerificationExpirationCutoffUtc(DateTime utcNow)
    {
        return NormalizeToUtc(utcNow).AddYears(-1);
    }

    public static bool IsLocked(User user, IEnumerable<string> roles, DateTime utcNow)
    {
        if (user is null || roles is null || !roles.Contains(Role.Constants.Lector))
        {
            return false;
        }

        return user.RequiresAnnualVerification(utcNow);
    }

    public static void EnsureCanUnlock(User targetUser, IEnumerable<string> targetRoles, DateTime utcNow)
    {
        Contracts.Require(targetUser is not null, "Target user cannot be null.");
        Contracts.Require(targetRoles != null && targetRoles.Contains(Role.Constants.Lector),
            "Only teachers (lectors) can be unlocked.");
        Contracts.Require(targetUser.RequiresAnnualVerification(utcNow),
            "Lector account is already verified and does not need unlocking.");
    }

    internal static DateTime NormalizeToUtc(DateTime timestamp)
    {
        return timestamp.Kind == DateTimeKind.Local
            ? timestamp.ToUniversalTime()
            : DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }
}
