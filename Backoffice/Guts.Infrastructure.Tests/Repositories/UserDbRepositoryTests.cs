using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.RoleAggregate;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class UserDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetUsersOfTopicAsync_And_GetUsersOfCourseForCurrentPeriodAsync_ShouldReturnOrderedUsers()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var userB = await Seeder.SeedUserAsync("Bob", "Jones");
        var userA = await Seeder.SeedUserAsync("Alice", "Smith");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id);
        var assignment = chapter.Assignments.First();
        var testId = assignment.Tests.First().Id;

        await Seeder.SeedTestRunAsync(assignment.Id, userB.Id, DateTime.UtcNow, (testId, true));
        await Seeder.SeedTestRunAsync(assignment.Id, userA.Id, DateTime.UtcNow, (testId, true));

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var topicUsers = await repo.GetUsersOfTopicAsync(chapter.Id);
        Assert.That(topicUsers.Select(u => u.FirstName), Is.EqualTo(["Alice", "Bob"]));

        var courseUsers = await repo.GetUsersOfCourseForCurrentPeriodAsync(course.Id);
        Assert.That(courseUsers.Select(u => u.FirstName), Is.EqualTo(["Alice", "Bob"]));
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        var user = await Seeder.SeedUserAsync("Charlie", "Brown");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var result = await repo.GetByIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(user.Id));
        Assert.That(result.FirstName, Is.EqualTo("Charlie"));
        Assert.That(result.LastName, Is.EqualTo("Brown"));
    }

    [Test]
    public async Task SearchUsersAndCourseLinks_ShouldIdentifyLinkedAndOrphanedUsers()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("USR1");
        var linkedUser = await Seeder.SeedUserAsync("Linked", "Student", "linked@student.pxl.be");
        var OrphanedUser = await Seeder.SeedUserAsync("Orphaned", "Student", "Orphaned@student.pxl.be");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id);
        var assignment = chapter.Assignments.First();
        var testId = assignment.Tests.First().Id;
        await Seeder.SeedTestRunAsync(assignment.Id, linkedUser.Id, DateTime.UtcNow, (testId, true));

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var linked = await repo.GetCourseLinksAsync(linkedUser.Id);
        var Orphaned = await repo.GetCourseLinksAsync(OrphanedUser.Id);
        var OrphanedResults = await repo.SearchUsersAsync("Orphaned", null, orphanedOnly: true);

        Assert.That(linked.HasAnyLinks, Is.True);
        Assert.That(Orphaned.HasAnyLinks, Is.False);
        Assert.That(OrphanedResults.TotalCount, Is.EqualTo(1));
        Assert.That(OrphanedResults.Items.Select(u => u.User.Id), Is.EquivalentTo([OrphanedUser.Id]));

        var linkedSearch = await repo.SearchUsersAsync("linked@student.pxl.be", null, orphanedOnly: false);
        Assert.That(linkedSearch.Items, Has.Count.EqualTo(1));
        Assert.That(linkedSearch.Items[0].LinkedCourses, Has.Count.EqualTo(1));
        Assert.That(linkedSearch.Items[0].LinkedCourses[0].Id, Is.EqualTo(course.Id));
        Assert.That(linkedSearch.Items[0].LinkedCourses[0].Code, Is.EqualTo(course.Code));
    }

    [Test]
    public async Task SearchUsersAsync_ShouldPaginateUsingSkipAndTakeAndSupportAll()
    {
        string prefix = "PagedTest_" + Guid.NewGuid().ToString("N")[..6];
        await Seeder.SeedUserAsync("Alice", prefix + "_A", prefix + "a@student.pxl.be");
        await Seeder.SeedUserAsync("Bob", prefix + "_B", prefix + "b@student.pxl.be");
        await Seeder.SeedUserAsync("Charlie", prefix + "_C", prefix + "c@student.pxl.be");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var page1 = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 1, pageSize: 2);
        var page2 = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 2, pageSize: 2);
        var all = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 1, pageSize: null);

        Assert.That(page1.TotalCount, Is.EqualTo(3));
        Assert.That(page1.Items.Select(x => x.User.FirstName), Is.EqualTo(["Alice", "Bob"]));
        Assert.That(page2.TotalCount, Is.EqualTo(3));
        Assert.That(page2.Items.Select(x => x.User.FirstName), Is.EqualTo(["Charlie"]));
        Assert.That(all.TotalCount, Is.EqualTo(3));
        Assert.That(all.Items.Select(x => x.User.FirstName), Is.EqualTo(["Alice", "Bob", "Charlie"]));
    }

    [Test]
    public async Task DeleteRangeAsync_ShouldRemoveMultipleUsers()
    {
        var user1 = await Seeder.SeedUserAsync("Del1", "User", $"del1_{Guid.NewGuid():N}@student.pxl.be");
        var user2 = await Seeder.SeedUserAsync("Del2", "User", $"del2_{Guid.NewGuid():N}@student.pxl.be");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var loaded = await repo.GetWithRolesAndLinksByIdsAsync([user1.Id, user2.Id]);
        Assert.That(loaded, Has.Count.EqualTo(2));

        await repo.DeleteRangeAsync(loaded.Select(x => x.User).ToList());

        Assert.That(await repo.GetByIdAsync(user1.Id), Is.Null);
        Assert.That(await repo.GetByIdAsync(user2.Id), Is.Null);
    }

    [Test]
    public async Task SearchUsersAsync_WithExcludedRoles_ShouldExcludeMatchingUsersBeforeCountingAndPaging()
    {
        string prefix = "Excl_" + Guid.NewGuid().ToString("N")[..6];
        var student1 = await Seeder.SeedUserAsync("Alice", prefix + "_A", prefix + "a@student.pxl.be");
        var student2 = await Seeder.SeedUserAsync("Bob", prefix + "_B", prefix + "b@student.pxl.be");
        var dualRole = await Seeder.SeedUserAsync("Carl", prefix + "_C", prefix + "c@pxl.be");

        await using (var seedContext = CreateDbContext())
        {
            var studentRole = new Role { Name = Role.Constants.Student, NormalizedName = Role.Constants.Student.ToUpperInvariant() };
            var lectorRole = new Role { Name = Role.Constants.Lector, NormalizedName = Role.Constants.Lector.ToUpperInvariant() };
            seedContext.Roles.AddRange(studentRole, lectorRole);
            await seedContext.SaveChangesAsync();

            seedContext.UserRoles.AddRange(
                new IdentityUserRole<int> { UserId = student1.Id, RoleId = studentRole.Id },
                new IdentityUserRole<int> { UserId = student2.Id, RoleId = studentRole.Id },
                new IdentityUserRole<int> { UserId = dualRole.Id, RoleId = studentRole.Id },
                new IdentityUserRole<int> { UserId = dualRole.Id, RoleId = lectorRole.Id });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var page1 = await repo.SearchUsersAsync(
            prefix,
            Role.Constants.Student,
            orphanedOnly: false,
            excludedRoles: [Role.Constants.Admin, Role.Constants.Lector],
            page: 1,
            pageSize: 1);
        var page2 = await repo.SearchUsersAsync(
            prefix,
            Role.Constants.Student,
            orphanedOnly: false,
            excludedRoles: [Role.Constants.Admin, Role.Constants.Lector],
            page: 2,
            pageSize: 1);

        Assert.That(page1.TotalCount, Is.EqualTo(2));
        Assert.That(page1.Items.Select(x => x.User.Id), Is.EqualTo([student1.Id]));
        Assert.That(page2.TotalCount, Is.EqualTo(2));
        Assert.That(page2.Items.Select(x => x.User.Id), Is.EqualTo([student2.Id]));
    }

    [Test]
    public async Task DeleteRangeAsync_WhenUserHasCourseLinks_ShouldDeleteUserAndAllCourseLinks()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("DEL1");
        var linkedStudent = await Seeder.SeedUserAsync("Linked", "One", $"linked_{Guid.NewGuid():N}@student.pxl.be");
        var peerStudent = await Seeder.SeedUserAsync("Peer", "Two", $"peer_{Guid.NewGuid():N}@student.pxl.be");

        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id);
        var chapterAssignment = chapter.Assignments.First();
        var chapterTestId = chapterAssignment.Tests.First().Id;
        await Seeder.SeedTestRunAsync(chapterAssignment.Id, linkedStudent.Id, DateTime.UtcNow, (chapterTestId, true));

        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            teams: ("TeamA", [linkedStudent.Id, peerStudent.Id]));
        var assessment = await Seeder.SeedProjectAssessmentAsync(project.Id);
        var teamAssessment = await Seeder.SeedProjectTeamAssessmentAsync(
            assessment.Id,
            project.Teams.First().Id,
            initialPeerEvaluation: (linkedStudent.Id, peerStudent.Id));

        await using (var seedContext = CreateDbContext())
        {
            var studentRole = new Role { Name = Role.Constants.Student, NormalizedName = Role.Constants.Student.ToUpperInvariant() };
            seedContext.Roles.Add(studentRole);
            await seedContext.SaveChangesAsync();

            seedContext.UserRoles.Add(new IdentityUserRole<int> { UserId = linkedStudent.Id, RoleId = studentRole.Id });
            seedContext.UserClaims.Add(new IdentityUserClaim<int> { UserId = linkedStudent.Id, ClaimType = "test", ClaimValue = "val" });

            var trackedAssessment = await seedContext.ProjectTeamAssessments
                .Include(pta => pta.Peers)
                .Include(pta => pta.PeerAssessments)
                .ThenInclude(pa => pa.User)
                .Include(pta => pta.PeerAssessments)
                .ThenInclude(pa => pa.Subject)
                .FirstAsync(pta => pta.Id == teamAssessment.Id);
            trackedAssessment.AddOrUpdatePeerAssessment(
                peerStudent.Id,
                linkedStudent.Id,
                AssessmentScore.Average,
                AssessmentScore.Average,
                AssessmentScore.Average,
                "Evaluated");

            seedContext.SolutionFiles.Add(
                SolutionFile.CreateNew(chapterAssignment.Id, linkedStudent.Id, new FilePath("Program.cs"), "// code"));
            var profile = new StudentCourseProfile(linkedStudent.Id, course.Id, period.Id);
            seedContext.StudentCourseProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            seedContext.GamificationEvents.Add(new GamificationEvent(
                profile.Id,
                GamificationEventType.PassedTest,
                chapterTestId,
                10,
                DateTime.UtcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);
        var target = await repo.GetByIdAsync(linkedStudent.Id);

        await repo.DeleteAsync(target);

        Assert.That(await repo.GetByIdAsync(linkedStudent.Id), Is.Null);
        Assert.That(await context.TestRuns.AnyAsync(tr => tr.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.TestResults.AnyAsync(tr => tr.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.SolutionFiles.AnyAsync(sf => sf.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.ProjectTeamUsers.AnyAsync(ptu => ptu.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.StudentCourseProfiles.AnyAsync(sp => sp.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.GamificationEvents.AnyAsync(), Is.False);
        Assert.That(await context.UserRoles.AnyAsync(ur => ur.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.UserClaims.AnyAsync(uc => uc.UserId == linkedStudent.Id), Is.False);
        Assert.That(await context.PeerAssessments.AnyAsync(pa => pa.User.Id == linkedStudent.Id || pa.Subject.Id == linkedStudent.Id), Is.False);
        Assert.That(await repo.GetByIdAsync(peerStudent.Id), Is.Not.Null);
    }

    [Test]
    public async Task SearchUsersAsync_LockedOnlyTrue_And_UpdateAsync_ShouldFilterLockedLectorsAndPersistVerification()
    {
        var lockedNeverVerifiedLector = await Seeder.SeedUserAsync("Locked", "Never", "locked.never@pxl.be");
        var lockedExpiredLector = await Seeder.SeedUserAsync("Locked", "Expired", "locked.expired@pxl.be");
        var verifiedLector = await Seeder.SeedUserAsync("Verified", "Lector", "verified@pxl.be");
        var student = await Seeder.SeedUserAsync("Student", "User", "student@student.pxl.be");

        await using (var seedContext = CreateDbContext())
        {
            var lectorRole = new Role { Name = Role.Constants.Lector, NormalizedName = Role.Constants.Lector.ToUpperInvariant() };
            var studentRole = new Role { Name = Role.Constants.Student, NormalizedName = Role.Constants.Student.ToUpperInvariant() };
            seedContext.Roles.AddRange(lectorRole, studentRole);
            await seedContext.SaveChangesAsync();

            seedContext.UserRoles.AddRange(
                new IdentityUserRole<int> { UserId = lockedNeverVerifiedLector.Id, RoleId = lectorRole.Id },
                new IdentityUserRole<int> { UserId = lockedExpiredLector.Id, RoleId = lectorRole.Id },
                new IdentityUserRole<int> { UserId = verifiedLector.Id, RoleId = lectorRole.Id },
                new IdentityUserRole<int> { UserId = student.Id, RoleId = studentRole.Id });

            var expiredEntity = await seedContext.Users.FirstAsync(u => u.Id == lockedExpiredLector.Id);
            expiredEntity.MarkEmailVerified(DateTime.UtcNow.AddYears(-1).AddMinutes(-5));

            var verifiedEntity = await seedContext.Users.FirstAsync(u => u.Id == verifiedLector.Id);
            verifiedEntity.MarkEmailVerified(DateTime.UtcNow.AddDays(-10));

            await seedContext.SaveChangesAsync();
        }

        await using (var queryContext = CreateDbContext())
        {
            var repo = new UserDbRepository(queryContext);
            var lockedResult = await repo.SearchUsersAsync(null, null, false, lockedOnly: true, page: 1, pageSize: null);

            Assert.That(lockedResult.TotalCount, Is.EqualTo(2));
            Assert.That(lockedResult.Items.Select(x => x.User.Id), Is.EquivalentTo(new[]
            {
                lockedNeverVerifiedLector.Id,
                lockedExpiredLector.Id
            }));

            var trackedLocked = await repo.GetByIdAsync(lockedNeverVerifiedLector.Id);
            trackedLocked.MarkEmailVerified(DateTime.UtcNow);
            await repo.UpdateAsync(trackedLocked);
        }

        await using (var verifyContext = CreateDbContext())
        {
            var repo = new UserDbRepository(verifyContext);
            var lockedAfterUnlock = await repo.SearchUsersAsync(null, null, false, lockedOnly: true, page: 1, pageSize: null);

            Assert.That(lockedAfterUnlock.TotalCount, Is.EqualTo(1));
            Assert.That(lockedAfterUnlock.Items.Single().User.Id, Is.EqualTo(lockedExpiredLector.Id));
        }
    }
}
