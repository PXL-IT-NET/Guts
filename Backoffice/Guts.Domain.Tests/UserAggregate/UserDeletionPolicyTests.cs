using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate;

[TestFixture]
public class UserDeletionPolicyTests
{
    [Test]
    public void UserCourseLinks_WhenAllFalse_HasAnyLinksIsFalse()
    {
        var links = new UserCourseLinks(false, false, false, false, false, false);

        Assert.That(links.HasAnyLinks, Is.False);
    }

    [TestCase(true, false, false, false, false, false)]
    [TestCase(false, true, false, false, false, false)]
    [TestCase(false, false, true, false, false, false)]
    [TestCase(false, false, false, true, false, false)]
    [TestCase(false, false, false, false, true, false)]
    [TestCase(false, false, false, false, false, true)]
    public void UserCourseLinks_WhenAnyTrue_HasAnyLinksIsTrue(
        bool hasTestRuns,
        bool hasTestResults,
        bool hasSolutionFiles,
        bool hasProjectTeams,
        bool hasProfiles,
        bool hasPeerAssessments)
    {
        var links = new UserCourseLinks(hasTestRuns, hasTestResults, hasSolutionFiles, hasProjectTeams, hasProfiles, hasPeerAssessments);

        Assert.That(links.HasAnyLinks, Is.True);
    }

    [Test]
    public void EnsureCanBeDeleted_OrphanedNonAdminNotSelf_DoesNotThrow()
    {
        var target = new User { Id = 42, Email = "old@student.pxl.be" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        Assert.DoesNotThrow(() =>
            UserDeletionPolicy.EnsureCanBeDeleted(target, currentUserId: 1, [Role.Constants.Student], links));
    }

    [Test]
    public void EnsureCanBeDeleted_Self_ThrowsContractException()
    {
        var target = new User { Id = 1, Email = "me@pxl.be" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        Assert.Throws<ContractException>(() =>
            UserDeletionPolicy.EnsureCanBeDeleted(target, currentUserId: 1, [Role.Constants.Lector], links));
    }

    [Test]
    public void EnsureCanBeDeleted_AdminUser_ThrowsContractException()
    {
        var target = new User { Id = 42, Email = "admin@pxl.be" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        Assert.Throws<ContractException>(() =>
            UserDeletionPolicy.EnsureCanBeDeleted(target, currentUserId: 1, [Role.Constants.Admin], links));
    }

    [Test]
    public void EnsureCanBeDeleted_UserWithCourseLinks_ThrowsContractException()
    {
        var target = new User { Id = 42, Email = "active@student.pxl.be" };
        var links = new UserCourseLinks(false, true, false, false, false, false);

        Assert.Throws<ContractException>(() =>
            UserDeletionPolicy.EnsureCanBeDeleted(target, currentUserId: 1, new List<string> { Role.Constants.Student }, links));
    }
}
