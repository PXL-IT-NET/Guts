using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services;

[TestFixture]
public class GamificationServiceTests
{
    private GamificationService _service;
    private Mock<IStudentCourseProfileRepository> _studentCourseProfileRepositoryMock;
    private Mock<ICourseGamificationConfigRepository> _courseGamificationConfigRepositoryMock;
    private Mock<ICourseClassGroupRepository> _courseClassGroupRepositoryMock;
    private Mock<ICourseRepository> _courseRepositoryMock;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
    private Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<IExamRepository> _examRepositoryMock;
    private Mock<IStudentCourseProfileFactory> _studentCourseProfileFactoryMock;
    private Mock<ICourseClassGroupFactory> _courseClassGroupFactoryMock;
    private Mock<ILogger<GamificationService>> _loggerMock;

    private Mock<IPeriod> _periodMock;

    [SetUp]
    public void Setup()
    {
        _studentCourseProfileRepositoryMock = new Mock<IStudentCourseProfileRepository>();
        _courseGamificationConfigRepositoryMock = new Mock<ICourseGamificationConfigRepository>();
        _courseClassGroupRepositoryMock = new Mock<ICourseClassGroupRepository>();
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _examRepositoryMock = new Mock<IExamRepository>();
        _studentCourseProfileFactoryMock = new Mock<IStudentCourseProfileFactory>();
        _courseClassGroupFactoryMock = new Mock<ICourseClassGroupFactory>();
        _loggerMock = new Mock<ILogger<GamificationService>>();

        _periodMock = new Mock<IPeriod>();
        _periodMock.SetupGet(p => p.Id).Returns(1);
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(It.IsAny<int?>())).ReturnsAsync(_periodMock.Object);

        _service = new GamificationService(
            _studentCourseProfileRepositoryMock.Object,
            _courseGamificationConfigRepositoryMock.Object,
            _courseClassGroupRepositoryMock.Object,
            _courseRepositoryMock.Object,
            _projectTeamRepositoryMock.Object,
            _assignmentRepositoryMock.Object,
            _periodRepositoryMock.Object,
            _studentCourseProfileFactoryMock.Object,
            _courseClassGroupFactoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task ProcessTestRunAsync_NoPassedTests_ShouldDoNothing()
    {
        var testRun = new TestRun
        {
            TestResults = new List<TestResult>
            {
                new TestResult { Passed = false }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        _assignmentRepositoryMock.Verify(r => r.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ProcessTestRunAsync_AssignmentCompleted_FirstStudent_ShouldAwardQuickBonusAndSpeedDemonBadge()
    {
        const int assignmentId = 42;
        const int courseId = 10;
        const int periodId = 1;
        const int userId = 5;

        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = userId,
            CreateDateTime = DateTime.UtcNow,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 101, Passed = true }
            }
        };

        var chapter = new Chapter { Id = 7, CourseId = courseId, PeriodId = periodId };
        var assignment = new Assignment
        {
            Id = assignmentId,
            TopicId = 7,
            Topic = chapter,
            Tests = new List<Test> { new Test { Id = 101 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsBadgesEnabled = true,
                IsWeeklyStreakEnabled = true
            });

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);

        // 0 students have completed it before => this student is 1st!
        _studentCourseProfileRepositoryMock.Setup(r => r.CountAssignmentCompletionsAsync(assignmentId, courseId, periodId))
            .ReturnsAsync(0);

        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(7))
            .ReturnsAsync(new List<Assignment> { assignment, new Assignment { Id = 99, Tests = new List<Test> { new Test { Id = 202 } } } });

        await _service.ProcessTestRunAsync(testRun);

        // 10 XP (pass test) + 50 XP (assignment bonus) + 50 XP (quick bonus for 1st) = 110 XP
        Assert.That(profile.TotalXp, Is.EqualTo(110));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.SpeedDemon), Is.True);
        Assert.That(profile.Events.Any(e => e.EventType == GamificationEventType.AssignmentCompletedQuickly && e.XpEarned == 50), Is.True);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile), Times.Once);
    }

    [Test]
    public async Task ProcessTestRunAsync_ComponentAllTestsGreenAndAllTeamMembersContributed_ShouldAwardTeamSynergyBonusToAllMembers()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int projectId = 20;
        const int componentId = 50;
        const int user1Id = 10;
        const int user2Id = 20;
        var now = DateTime.UtcNow;

        var project = new Project(projectId) { CourseId = courseId, PeriodId = periodId };
        var component = new Assignment
        {
            Id = componentId,
            TopicId = projectId,
            Topic = project,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(componentId)).ReturnsAsync(component);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsTeamSynergyEnabled = true,
                IsBadgesEnabled = true
            });

        var teamMock = new Mock<IProjectTeam>();
        teamMock.SetupGet(t => t.ProjectId).Returns(projectId);
        teamMock.SetupGet(t => t.TeamUsers).Returns(new List<IProjectTeamUser>
        {
            new ProjectTeamUser { UserId = user1Id },
            new ProjectTeamUser { UserId = user2Id }
        });
        _projectTeamRepositoryMock.Setup(r => r.GetByUserAsync(user1Id))
            .ReturnsAsync(new List<IProjectTeam> { teamMock.Object });

        // User 2 already passed test 1 earlier
        var profile2 = new StudentCourseProfile(user2Id, courseId, periodId);
        profile2.AwardPassedTestXp(1, now.AddHours(-1));

        // User 1 profile (already passed test 1 earlier, now submitting test run that passes test 2 to make all tests green)
        var profile1 = new StudentCourseProfile(user1Id, courseId, periodId);
        profile1.AwardPassedTestXp(1, now.AddHours(-1));

        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(projectId))
            .ReturnsAsync(new List<Assignment> { component });
        _studentCourseProfileRepositoryMock.Setup(r => r.CountAssignmentCompletionsAsync(componentId, courseId, periodId))
            .ReturnsAsync(0);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user1Id, courseId, periodId)).ReturnsAsync(profile1);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user2Id, courseId, periodId)).ReturnsAsync(profile2);

        var testRun = new TestRun
        {
            AssignmentId = componentId,
            UserId = user1Id,
            CreateDateTime = now,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        // Both members should have received TeamSynergy bonus (+30 XP) and TeamPowerhouse badge
        Assert.That(profile1.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy && e.TargetId == componentId), Is.True);
        Assert.That(profile1.Badges.Any(b => b.BadgeType == BadgeType.TeamPowerhouse), Is.True);

        Assert.That(profile2.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy && e.TargetId == componentId), Is.True);
        Assert.That(profile2.Badges.Any(b => b.BadgeType == BadgeType.TeamPowerhouse), Is.True);

        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile2), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile1), Times.Once);
    }

    [Test]
    public async Task ProcessTestRunAsync_ComponentAllTestsGreen_ButOneMemberHasZeroTests_ShouldNotAwardTeamSynergyBonus()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int projectId = 20;
        const int componentId = 50;
        const int user1Id = 10;
        const int user2Id = 20;
        var now = DateTime.UtcNow;

        var project = new Project(projectId) { CourseId = courseId, PeriodId = periodId };
        var component = new Assignment
        {
            Id = componentId,
            TopicId = projectId,
            Topic = project,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(componentId)).ReturnsAsync(component);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsTeamSynergyEnabled = true,
                IsBadgesEnabled = true
            });

        var teamMock = new Mock<IProjectTeam>();
        teamMock.SetupGet(t => t.ProjectId).Returns(projectId);
        teamMock.SetupGet(t => t.TeamUsers).Returns(new List<IProjectTeamUser>
        {
            new ProjectTeamUser { UserId = user1Id },
            new ProjectTeamUser { UserId = user2Id }
        });
        _projectTeamRepositoryMock.Setup(r => r.GetByUserAsync(user1Id))
            .ReturnsAsync(new List<IProjectTeam> { teamMock.Object });

        // User 2 has NO tests passed for this component
        var profile2 = new StudentCourseProfile(user2Id, courseId, periodId);

        // User 1 passes BOTH tests 1 and 2 alone
        var profile1 = new StudentCourseProfile(user1Id, courseId, periodId);
        profile1.AwardPassedTestXp(1, now.AddHours(-1));

        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(projectId))
            .ReturnsAsync(new List<Assignment> { component });

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user1Id, courseId, periodId)).ReturnsAsync(profile1);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user2Id, courseId, periodId)).ReturnsAsync(profile2);

        var testRun = new TestRun
        {
            AssignmentId = componentId,
            UserId = user1Id,
            CreateDateTime = now,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        // Team Synergy MUST NOT be awarded because user 2 has made 0 tests green!
        Assert.That(profile1.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        Assert.That(profile2.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile2), Times.Never);
    }

    [Test]
    public async Task ProcessTestRunAsync_NotAllComponentTestsGreen_ShouldNotAwardTeamSynergyBonus()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int projectId = 20;
        const int componentId = 50;
        const int user1Id = 10;
        const int user2Id = 20;
        var now = DateTime.UtcNow;

        var project = new Project(projectId) { CourseId = courseId, PeriodId = periodId };
        // Component has 3 tests, but only tests 1 and 2 are passed
        var component = new Assignment
        {
            Id = componentId,
            TopicId = projectId,
            Topic = project,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 }, new Test { Id = 3 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(componentId)).ReturnsAsync(component);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsTeamSynergyEnabled = true,
                IsBadgesEnabled = true
            });

        var teamMock = new Mock<IProjectTeam>();
        teamMock.SetupGet(t => t.ProjectId).Returns(projectId);
        teamMock.SetupGet(t => t.TeamUsers).Returns(new List<IProjectTeamUser>
        {
            new ProjectTeamUser { UserId = user1Id },
            new ProjectTeamUser { UserId = user2Id }
        });
        _projectTeamRepositoryMock.Setup(r => r.GetByUserAsync(user1Id))
            .ReturnsAsync(new List<IProjectTeam> { teamMock.Object });

        var profile2 = new StudentCourseProfile(user2Id, courseId, periodId);
        profile2.AwardPassedTestXp(1, now.AddHours(-1));

        var profile1 = new StudentCourseProfile(user1Id, courseId, periodId);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user1Id, courseId, periodId)).ReturnsAsync(profile1);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user2Id, courseId, periodId)).ReturnsAsync(profile2);

        var testRun = new TestRun
        {
            AssignmentId = componentId,
            UserId = user1Id,
            CreateDateTime = now,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        // Test 3 is not green -> no team synergy bonus!
        Assert.That(profile1.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        Assert.That(profile2.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
    }

    [Test]
    public async Task ProcessTestRunAsync_TeamSynergyDisabled_ShouldNotAwardTeamSynergyBonus()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int projectId = 20;
        const int componentId = 50;
        const int user1Id = 10;
        const int user2Id = 20;
        var now = DateTime.UtcNow;

        var project = new Project(projectId) { CourseId = courseId, PeriodId = periodId };
        var component = new Assignment
        {
            Id = componentId,
            TopicId = projectId,
            Topic = project,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(componentId)).ReturnsAsync(component);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsTeamSynergyEnabled = false,
                IsBadgesEnabled = true
            });

        var profile2 = new StudentCourseProfile(user2Id, courseId, periodId);
        profile2.AwardPassedTestXp(1, now.AddHours(-1));
        var profile1 = new StudentCourseProfile(user1Id, courseId, periodId);
        profile1.AwardPassedTestXp(1, now.AddHours(-1));

        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(projectId))
            .ReturnsAsync(new List<Assignment> { component });

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user1Id, courseId, periodId)).ReturnsAsync(profile1);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user2Id, courseId, periodId)).ReturnsAsync(profile2);

        var testRun = new TestRun
        {
            AssignmentId = componentId,
            UserId = user1Id,
            CreateDateTime = now,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        _projectTeamRepositoryMock.Verify(r => r.GetByUserAsync(It.IsAny<int>()), Times.Never);
        Assert.That(profile1.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        Assert.That(profile2.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
    }

    [Test]
    public async Task ProcessTestRunAsync_AssignmentTopicIsNull_ShouldSkipProcessing()
    {
        const int assignmentId = 42;
        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = 5,
            CreateDateTime = DateTime.UtcNow,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 1, Passed = true }
            }
        };

        var assignment = new Assignment
        {
            Id = assignmentId,
            Topic = null
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);

        await _service.ProcessTestRunAsync(testRun);

        _courseGamificationConfigRepositoryMock.Verify(r => r.GetByCourseIdAsync(It.IsAny<int>()), Times.Never);
        _studentCourseProfileRepositoryMock.Verify(r => r.GetByUserCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenUserHasNoTeamForTopic_ShouldNotAwardTeamSynergyBonus()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int chapterId = 20;
        const int assignmentId = 50;
        const int user1Id = 10;
        const int user2Id = 20;
        var now = DateTime.UtcNow;

        var chapter = new Chapter { Id = chapterId, CourseId = courseId, PeriodId = periodId };
        var assignment = new Assignment
        {
            Id = assignmentId,
            TopicId = chapterId,
            Topic = chapter,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);
        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(chapterId)).ReturnsAsync(new List<Assignment> { assignment });
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsTeamSynergyEnabled = true,
                IsBadgesEnabled = true
            });

        _projectTeamRepositoryMock.Setup(r => r.GetByUserAsync(user1Id))
            .ReturnsAsync(new List<IProjectTeam>());

        var profile2 = new StudentCourseProfile(user2Id, courseId, periodId);
        profile2.AwardPassedTestXp(1, now.AddHours(-1));
        var profile1 = new StudentCourseProfile(user1Id, courseId, periodId);
        profile1.AwardPassedTestXp(1, now.AddHours(-1));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user1Id, courseId, periodId)).ReturnsAsync(profile1);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(user2Id, courseId, periodId)).ReturnsAsync(profile2);

        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = user1Id,
            CreateDateTime = now,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true }
            }
        };

        await _service.ProcessTestRunAsync(testRun);

        _projectTeamRepositoryMock.Verify(r => r.GetByUserAsync(user1Id), Times.Once);
        Assert.That(profile1.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        Assert.That(profile2.Events.Any(e => e.EventType == GamificationEventType.TeamSynergy), Is.False);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile2), Times.Never);
    }

    [Test]
    public async Task EnrollStudentAsync_ValidDto_ShouldEnrollAndReturnProfileDto()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId)).ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>())).ReturnsAsync((StudentCourseProfile p) => p);
        var classGroup = new CourseClassGroup("1TIN-C", courseId, periodId) { Id = 3 };
        _courseClassGroupRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(classGroup);

        var enrollDto = new EnrollStudentDto
        {
            ShowInLeaderBoard = true,
            UsePseudonym = true,
            Pseudonym = "ApexDev",
            Avatar = "SyntaxFox",
            ClassGroupId = 3
        };

        var result = await _service.EnrollStudentAsync(userId, courseId, enrollDto, periodId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ShowInLeaderBoard, Is.True);
        Assert.That(result.Pseudonym, Is.EqualTo("ApexDev"));
        Assert.That(result.Avatar, Is.EqualTo("SyntaxFox"));
        Assert.That(result.ClassGroupId, Is.EqualTo(3));
        Assert.That(result.ClassGroupName, Is.EqualTo("1TIN-C"));
        Assert.That(result.FirstName, Is.EqualTo("Bob"));
        Assert.That(result.LastName, Is.EqualTo("Builder"));
    }

    [Test]
    public async Task GetClassGroupLeaderboardAsync_ShouldCalculateAverageXpCorrectly()
    {
        const int courseId = 10;
        const int periodId = 1;

        var groupA = new CourseClassGroup("1TIN-A", courseId, periodId) { Id = 1 };
        var groupB = new CourseClassGroup("1TIN-B", courseId, periodId) { Id = 2 };

        _courseClassGroupRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<CourseClassGroup> { groupA, groupB });

        var student1 = new StudentCourseProfile(101, courseId, periodId);
        student1.Enroll(true, "Student1", "CodeCat", 1, true);
        student1.AwardAssignmentBonus(1, DateTime.UtcNow); // +50 XP

        var student2 = new StudentCourseProfile(102, courseId, periodId);
        student2.Enroll(true, "Student2", "CodeCat", 1, true);
        student2.AwardChapterBonus(1, DateTime.UtcNow); // +100 XP

        var student3 = new StudentCourseProfile(103, courseId, periodId);
        student3.Enroll(true, "Student3", "CodeCat", 2, true);
        student3.AwardPassedTestXp(1, DateTime.UtcNow); // +10 XP

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { student1, student2, student3 });

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsClassCompetitionEnabled = true });

        var result = await _service.GetClassGroupLeaderboardAsync(courseId, periodId);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].ClassGroupName, Is.EqualTo("1TIN-A"));
        Assert.That(result[0].Rank, Is.EqualTo(1));
        Assert.That(result[0].AverageXp, Is.EqualTo(75.0));
        Assert.That(result[1].ClassGroupName, Is.EqualTo("1TIN-B"));
        Assert.That(result[1].Rank, Is.EqualTo(2));
        Assert.That(result[1].AverageXp, Is.EqualTo(10.0));
    }

    [Test]
    public void AddCourseClassGroupAsync_DuplicateName_ShouldThrowContractException()
    {
        const int courseId = 10;
        const int periodId = 1;
        const string className = "1TIN-A";

        _courseClassGroupRepositoryMock.Setup(r => r.GetByNameAsync(courseId, periodId, className))
            .ReturnsAsync(new CourseClassGroup(className, courseId, periodId));

        Assert.ThrowsAsync<ContractException>(async () =>
            await _service.AddCourseClassGroupAsync(courseId, className, periodId));
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_ShouldCalculateRecentGainsAndRecordVisit()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var previousVisit = DateTime.UtcNow.AddDays(-2);
        profile.RecordLeaderboardVisit(previousVisit);

        profile.AwardPassedTestXp(1, previousVisit.AddDays(-1));
        profile.AwardAssignmentBonus(10, previousVisit.AddDays(1));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecentGains, Is.Not.Null);
        Assert.That(result.RecentGains.PreviousVisitDate, Is.EqualTo(previousVisit));
        Assert.That(result.RecentGains.TotalXpGained, Is.EqualTo(50));
        Assert.That(result.RecentGains.Events.Count, Is.EqualTo(1));
        Assert.That(result.RecentGains.Events[0].XpEarned, Is.EqualTo(50));
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(It.Is<StudentCourseProfile>(p => p.LastLeaderboardVisitDate.HasValue && p.LastLeaderboardVisitDate.Value > previousVisit)), Times.Once);
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WhenLastVisitWithin10Minutes_ShouldNotUpdateLeaderboardVisitDate()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var previousVisit = DateTime.UtcNow.AddMinutes(-5);
        profile.RecordLeaderboardVisit(previousVisit);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastLeaderboardVisitDate, Is.EqualTo(previousVisit));
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()), Times.Never);
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenGamificationDisabled_ShouldSkipProcessing()
    {
        const int assignmentId = 42;
        const int courseId = 10;
        const int periodId = 1;

        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = 5,
            CreateDateTime = DateTime.UtcNow,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 1, Passed = true }
            }
        };

        var chapter = new Chapter { CourseId = courseId, PeriodId = periodId };
        var assignment = new Assignment { Id = assignmentId, Topic = chapter };
        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = false });

        await _service.ProcessTestRunAsync(testRun);

        _studentCourseProfileRepositoryMock.Verify(r => r.GetByUserCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetWeeklySprintLeaderboardAsync_WhenDisabled_ShouldReturnEmptyList()
    {
        const int courseId = 10;
        const int periodId = 1;

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsWeeklyStreakEnabled = false });

        var result = await _service.GetWeeklySprintLeaderboardAsync(courseId, periodId);

        Assert.That(result, Is.Empty);
        _studentCourseProfileRepositoryMock.Verify(r => r.GetForWeeklySprintLeaderboardAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Test]
    public async Task GetHallOfFameLeaderboardAsync_WhenDisabled_ShouldReturnEmptyList()
    {
        const int courseId = 10;
        const int periodId = 1;

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsLeaderboardEnabled = false });

        var result = await _service.GetHallOfFameLeaderboardAsync(courseId, periodId);

        Assert.That(result.Entries, Is.Empty);
        _studentCourseProfileRepositoryMock.Verify(r => r.GetForLeaderboardByCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetClassGroupLeaderboardAsync_WhenDisabled_ShouldReturnEmptyList()
    {
        const int courseId = 10;
        const int periodId = 1;

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsClassCompetitionEnabled = false });

        var result = await _service.GetClassGroupLeaderboardAsync(courseId, periodId);

        Assert.That(result, Is.Empty);
        _courseClassGroupRepositoryMock.Verify(r => r.GetByCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task AwardWeeklySprintBadgesAsync_ShouldAwardSprintGoldAndRankBadges_OnlyIfXpGreaterThanZero()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int year = 2026;
        const int weekNumber = 43;
        var eventTime = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        var weekendEventTime = new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

        var course = new Course { Id = courseId, Code = "CSharp" };
        _courseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { course });
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsWeeklyStreakEnabled = true,
                IsLeaderboardEnabled = true,
                IsBadgesEnabled = true
            });

        var s1 = new StudentCourseProfile(1, courseId, periodId);
        s1.AwardPassedTestXp(101, eventTime);
        s1.AwardAssignmentBonus(201, eventTime);

        var s2 = new StudentCourseProfile(2, courseId, periodId);
        s2.AwardAssignmentBonus(202, eventTime);

        var s3 = new StudentCourseProfile(3, courseId, periodId);
        s3.AwardPassedTestXp(103, eventTime);
        s3.AwardPassedTestXp(104, eventTime);
        s3.AwardPassedTestXp(105, eventTime);
        s3.AwardPassedTestXp(106, eventTime);

        var s4 = new StudentCourseProfile(4, courseId, periodId);
        s4.AwardPassedTestXp(107, eventTime);
        s4.AwardPassedTestXp(108, eventTime);
        s4.AwardPassedTestXp(109, eventTime);

        var s5 = new StudentCourseProfile(5, courseId, periodId);
        s5.AwardPassedTestXp(110, eventTime);

        var s6ZeroXp = new StudentCourseProfile(6, courseId, periodId);

        var s7WeekendXp = new StudentCourseProfile(7, courseId, periodId);
        s7WeekendXp.AwardAssignmentBonus(207, weekendEventTime);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { s1, s2, s3, s4, s5, s6ZeroXp, s7WeekendXp });
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        await _service.AwardWeeklySprintBadgesAsync(year, weekNumber, periodId);

        Assert.That(s1.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.True);
        Assert.That(s1.Badges.Any(b => b.BadgeType == BadgeType.SprintSilver), Is.True);
        Assert.That(s1.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.True);
        Assert.That(s1.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.True);
        Assert.That(s1.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.True);
        Assert.That(s1.Badges.First(b => b.BadgeType == BadgeType.SprintGold).EarnedAt,
            Is.EqualTo(new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc)));

        Assert.That(s2.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.False);
        Assert.That(s2.Badges.Any(b => b.BadgeType == BadgeType.SprintSilver), Is.True);
        Assert.That(s2.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.True);
        Assert.That(s2.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.True);
        Assert.That(s2.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.True);

        Assert.That(s3.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.False);
        Assert.That(s3.Badges.Any(b => b.BadgeType == BadgeType.SprintSilver), Is.False);
        Assert.That(s3.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.True);
        Assert.That(s3.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.True);
        Assert.That(s3.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.True);

        Assert.That(s4.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.False);
        Assert.That(s4.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.True);
        Assert.That(s4.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.True);

        Assert.That(s5.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.False);
        Assert.That(s5.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.True);

        Assert.That(s6ZeroXp.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.False);
        Assert.That(s6ZeroXp.Badges.Any(b => b.BadgeType == BadgeType.SprintSilver), Is.False);
        Assert.That(s6ZeroXp.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.False);
        Assert.That(s6ZeroXp.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.False);
        Assert.That(s6ZeroXp.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.False);

        Assert.That(s7WeekendXp.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.False);
        Assert.That(s7WeekendXp.Badges.Any(b => b.BadgeType == BadgeType.SprintSilver), Is.False);
        Assert.That(s7WeekendXp.Badges.Any(b => b.BadgeType == BadgeType.SprintBronze), Is.False);
        Assert.That(s7WeekendXp.Badges.Any(b => b.BadgeType == BadgeType.SprintTop4), Is.False);
        Assert.That(s7WeekendXp.Badges.Any(b => b.BadgeType == BadgeType.SprintTop5), Is.False);

        _studentCourseProfileRepositoryMock.Verify(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId), Times.Once);
    }

    [Test]
    public async Task AwardWeeklySprintBadgesAsync_WhenGamificationDisabled_ShouldNotAwardBadges()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int year = 2026;
        const int weekNumber = 43;

        var course = new Course { Id = courseId, Code = "CSharp" };
        _courseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { course });
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = false,
                IsWeeklyStreakEnabled = true,
                IsLeaderboardEnabled = true,
                IsBadgesEnabled = true
            });

        await _service.AwardWeeklySprintBadgesAsync(year, weekNumber, periodId);

        _studentCourseProfileRepositoryMock.Verify(r => r.GetForLeaderboardByCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void GetBadgeMetadata_ShouldReturnAll15BadgeMetadataEntries()
    {
        var badges = _service.GetBadgeMetadata();

        Assert.That(badges, Is.Not.Null);
        Assert.That(badges.Count, Is.EqualTo(15));
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.FirstSpark && b.Icon == "🌱"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.SprintGold && b.Icon == "🥇"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.SprintSilver && b.Icon == "🥈"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.SprintBronze && b.Icon == "🥉"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.SprintTop4 && b.Icon == "🎖️"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.SprintTop5 && b.Icon == "⭐"), Is.True);
        Assert.That(badges.Any(b => b.BadgeType == BadgeType.TeamPowerhouse && b.Icon == "🤝"), Is.True);
        Assert.That(badges.All(b => b.Type == (int)b.BadgeType), Is.True);
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WhenProfileDoesNotExist_ShouldCreateAndReturnDto()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;
        var periodMock = new Mock<IPeriod>();
        periodMock.SetupGet(p => p.Id).Returns(periodId);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(periodMock.Object);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync((StudentCourseProfile)null);

        var createdProfile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Alice", LastName = "Wonder" }
        };
        createdProfile.AwardPassedTestXp(101, DateTime.UtcNow.AddMinutes(-5));
        createdProfile.AwardAssignmentBonus(201, DateTime.UtcNow.AddMinutes(-4));
        createdProfile.AwardChapterBonus(301, DateTime.UtcNow.AddMinutes(-3));
        createdProfile.AwardTeamSynergyBonus(401, DateTime.UtcNow.AddMinutes(-2));

        _studentCourseProfileFactoryMock.Setup(f => f.CreateNew(userId, courseId, periodId))
            .Returns(createdProfile);
        _studentCourseProfileRepositoryMock.Setup(r => r.AddAsync(createdProfile))
            .ReturnsAsync(createdProfile);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.RecentGains, Is.Not.Null);
        Assert.That(result.RecentGains.Events.Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public async Task GetHallOfFameLeaderboardAsync_WhenEnabled_ShouldReturnOrderedLeaderboard()
    {
        const int courseId = 10;
        const int periodId = 1;

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsLeaderboardEnabled = true });

        var periodMock = new Mock<IPeriod>();
        periodMock.SetupGet(p => p.Id).Returns(periodId);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(periodId)).ReturnsAsync(periodMock.Object);

        var p1 = new StudentCourseProfile(1, courseId, periodId)
        {
            User = new User { FirstName = "Alice", LastName = "One" }
        };
        p1.AwardPassedTestXp(1, DateTime.UtcNow);
        p1.AwardPassedTestXp(2, DateTime.UtcNow); // 20 XP

        var p2 = new StudentCourseProfile(2, courseId, periodId);
        p2.Enroll(true, "BobGhost", "Ninja", null, true); // UsePseudonym = true
        p2.AwardPassedTestXp(3, DateTime.UtcNow); // 10 XP

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { p2, p1 });

        var result = await _service.GetHallOfFameLeaderboardAsync(courseId, 1, periodId);

        Assert.That(result.Entries.Count, Is.EqualTo(2));
        Assert.That(result.Entries[0].UserId, Is.EqualTo(1));
        Assert.That(result.Entries[0].Score, Is.EqualTo(20));
        Assert.That(result.Entries[0].Rank, Is.EqualTo(1));
        Assert.That(result.Entries[0].IsCurrentUser, Is.True);

        Assert.That(result.Entries[1].UserId, Is.EqualTo(2));
        Assert.That(result.Entries[1].DisplayName, Is.EqualTo("BobGhost"));
    }

    [Test]
    public async Task GetCourseConfigAsync_WhenConfigExists_ShouldReturnDto()
    {
        const int courseId = 10;
        var config = new CourseGamificationConfig(courseId)
        {
            IsGamificationEnabled = true,
            IsWeeklyStreakEnabled = true,
            IsLeaderboardEnabled = true,
            IsClassCompetitionEnabled = true,
            IsTeamSynergyEnabled = true,
            IsBadgesEnabled = true
        };
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync(config);

        var dto = await _service.GetCourseConfigAsync(courseId);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.CourseId, Is.EqualTo(courseId));
        Assert.That(dto.IsGamificationEnabled, Is.True);
    }

    [Test]
    public async Task GetCourseConfigAsync_WhenConfigDoesNotExist_ShouldCreateDefaultAndReturnDto()
    {
        const int courseId = 10;
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync((CourseGamificationConfig)null);
        _courseGamificationConfigRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CourseGamificationConfig>()))
            .ReturnsAsync((CourseGamificationConfig c) => c);

        var dto = await _service.GetCourseConfigAsync(courseId);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.CourseId, Is.EqualTo(courseId));
        _courseGamificationConfigRepositoryMock.Verify(r => r.AddAsync(It.IsAny<CourseGamificationConfig>()), Times.Once);
    }

    [Test]
    public async Task UpdateCourseConfigAsync_WhenConfigExists_ShouldUpdateAndReturnDto()
    {
        const int courseId = 10;
        var existing = new CourseGamificationConfig(courseId) { IsGamificationEnabled = true };
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync(existing);
        _courseGamificationConfigRepositoryMock.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var updateDto = new CourseGamificationConfigDto
        {
            CourseId = courseId,
            IsGamificationEnabled = false,
            IsWeeklyStreakEnabled = false,
            IsLeaderboardEnabled = false,
            IsClassCompetitionEnabled = false,
            IsTeamSynergyEnabled = false,
            IsBadgesEnabled = false
        };

        var result = await _service.UpdateCourseConfigAsync(courseId, updateDto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsGamificationEnabled, Is.False);
        _courseGamificationConfigRepositoryMock.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Test]
    public async Task ClassGroupCrud_ShouldOperateCorrectly()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int classGroupId = 50;

        var periodMock = new Mock<IPeriod>();
        periodMock.SetupGet(p => p.Id).Returns(periodId);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(periodId)).ReturnsAsync(periodMock.Object);

        var group = new CourseClassGroup("1TIN-A", courseId, periodId) { Id = classGroupId };
        _courseClassGroupRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<CourseClassGroup> { group });

        var groups = await _service.GetCourseClassGroupsAsync(courseId, periodId);
        Assert.That(groups.Count, Is.EqualTo(1));
        Assert.That(groups[0].Name, Is.EqualTo("1TIN-A"));

        _courseClassGroupFactoryMock.Setup(f => f.CreateNew("1TIN-B", courseId, periodId))
            .Returns(new CourseClassGroup("1TIN-B", courseId, periodId) { Id = 51 });
        _courseClassGroupRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CourseClassGroup>()))
            .ReturnsAsync((CourseClassGroup g) => g);

        var created = await _service.AddCourseClassGroupAsync(courseId, "1TIN-B", periodId);
        Assert.That(created.Name, Is.EqualTo("1TIN-B"));

        _courseClassGroupRepositoryMock.Setup(r => r.GetByIdAsync(classGroupId)).ReturnsAsync(group);
        await _service.DeleteCourseClassGroupAsync(courseId, classGroupId);
        _courseClassGroupRepositoryMock.Verify(r => r.DeleteAsync(group), Times.Once);
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenExceptionThrown_ShouldLogAndSwallowException()
    {
        var testRun = new TestRun
        {
            Id = 99,
            AssignmentId = 42,
            TestResults = new List<TestResult> { new TestResult { TestId = 1, Passed = true } }
        };
        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(42))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        await _service.ProcessTestRunAsync(testRun);

        Assert.Pass();
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenProfileDoesNotExist_ShouldCreateAndAddProfile()
    {
        const int assignmentId = 42;
        const int courseId = 10;
        const int periodId = 1;
        const int userId = 5;

        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = userId,
            CreateDateTime = DateTime.UtcNow,
            TestResults = new List<TestResult> { new TestResult { TestId = 101, Passed = true } }
        };

        var chapter = new Chapter { Id = 7, CourseId = courseId, PeriodId = periodId };
        var assignment = new Assignment
        {
            Id = assignmentId,
            TopicId = 7,
            Topic = chapter,
            Tests = new List<Test> { new Test { Id = 101 }, new Test { Id = 102 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true });

        var newProfile = new StudentCourseProfile(userId, courseId, periodId);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync((StudentCourseProfile)null);
        _studentCourseProfileFactoryMock.Setup(f => f.CreateNew(userId, courseId, periodId)).Returns(newProfile);
        _studentCourseProfileRepositoryMock.Setup(r => r.AddAsync(newProfile)).ReturnsAsync(newProfile);

        await _service.ProcessTestRunAsync(testRun);

        _studentCourseProfileFactoryMock.Verify(f => f.CreateNew(userId, courseId, periodId), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(r => r.AddAsync(newProfile), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(newProfile), Times.Once);
    }

    [Test]
    public async Task EnrollStudentAsync_WhenProfileDoesNotExistAndNoClassGroup_ShouldCreateProfile()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var createdProfile = new StudentCourseProfile(userId, courseId, periodId);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync((StudentCourseProfile)null);
        _studentCourseProfileFactoryMock.Setup(f => f.CreateNew(userId, courseId, periodId)).Returns(createdProfile);
        _studentCourseProfileRepositoryMock.Setup(r => r.AddAsync(createdProfile)).ReturnsAsync(createdProfile);

        var enrollDto = new EnrollStudentDto
        {
            ShowInLeaderBoard = true,
            UsePseudonym = false,
            Pseudonym = "NewCoder",
            Avatar = "CodeCat",
            ClassGroupId = null
        };

        var result = await _service.EnrollStudentAsync(userId, courseId, enrollDto, periodId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ClassGroupId, Is.Null);
        _studentCourseProfileRepositoryMock.Verify(r => r.AddAsync(createdProfile), Times.Once);
    }

    [Test]
    public async Task GetWeeklySprintLeaderboardAsync_WhenEnabled_ShouldReturnLeaderboard()
    {
        const int courseId = 10;
        const int periodId = 1;

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsWeeklyStreakEnabled = true,
                IsLeaderboardEnabled = true
            });

        var p1 = new StudentCourseProfile(1, courseId, periodId);
        p1.Enroll(true, "FallbackPseudo", "Rocket", null, false);
        p1.AwardPassedTestXp(1, DateTime.Now.AddMinutes(-5));

        var p2ZeroWeeklyXp = new StudentCourseProfile(2, courseId, periodId);
        p2ZeroWeeklyXp.Enroll(true, "InactiveThisWeek", "CodeCat", null, true);
        p2ZeroWeeklyXp.AwardPassedTestXp(1, DateTime.Now.AddDays(-14));

        var p3NoXpEver = new StudentCourseProfile(3, courseId, periodId);
        p3NoXpEver.Enroll(true, "ZeroXpStudent", "Wizard", null, true);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForWeeklySprintLeaderboardAsync(courseId, periodId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<StudentCourseProfile> { p1, p2ZeroWeeklyXp, p3NoXpEver });

        var result = await _service.GetWeeklySprintLeaderboardAsync(courseId, 1, periodId);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].UserId, Is.EqualTo(1));
        Assert.That(result[0].DisplayName, Is.EqualTo("FallbackPseudo"));
    }

    [Test]
    public async Task UpdateCourseConfigAsync_WhenConfigDoesNotExist_ShouldAddAndReturnDto()
    {
        const int courseId = 10;
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync((CourseGamificationConfig)null);
        _courseGamificationConfigRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CourseGamificationConfig>()))
            .ReturnsAsync((CourseGamificationConfig c) => c);

        var updateDto = new CourseGamificationConfigDto
        {
            CourseId = courseId,
            IsGamificationEnabled = true,
            IsWeeklyStreakEnabled = true,
            IsLeaderboardEnabled = true,
            IsClassCompetitionEnabled = true,
            IsTeamSynergyEnabled = true,
            IsBadgesEnabled = true
        };

        var result = await _service.UpdateCourseConfigAsync(courseId, updateDto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsGamificationEnabled, Is.True);
        _courseGamificationConfigRepositoryMock.Verify(r => r.AddAsync(It.IsAny<CourseGamificationConfig>()), Times.Once);
    }

    [Test]
    public void DeleteCourseClassGroupAsync_WhenCourseIdMismatch_ShouldThrowDataNotFoundException()
    {
        var group = new CourseClassGroup("1TIN-A", 99, 1) { Id = 50 };
        _courseClassGroupRepositoryMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(group);

        Assert.ThrowsAsync<DataNotFoundException>(async () =>
            await _service.DeleteCourseClassGroupAsync(10, 50));
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WithMultiplePassedTests_ShouldSummarizePassedTestsIntoSingleEvent()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var previousVisit = DateTime.UtcNow.AddDays(-2);
        profile.RecordLeaderboardVisit(previousVisit);

        profile.AwardPassedTestXp(999, previousVisit.AddHours(-1));

        DateTime latestTestTime = previousVisit.AddHours(1);
        for (int i = 1; i <= 10; i++)
        {
            latestTestTime = previousVisit.AddHours(i);
            profile.AwardPassedTestXp(i, latestTestTime);
        }

        DateTime assignmentBonusTime = latestTestTime.AddMinutes(5);
        profile.AwardAssignmentBonus(10, assignmentBonusTime);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecentGains, Is.Not.Null);
        Assert.That(result.RecentGains.TotalXpGained, Is.EqualTo(150));
        Assert.That(result.RecentGains.Events.Count, Is.EqualTo(2));

        Assert.That(result.RecentGains.Events[0].Description, Is.EqualTo("Completed assignment bonus"));
        Assert.That(result.RecentGains.Events[0].XpEarned, Is.EqualTo(50));
        Assert.That(result.RecentGains.Events[0].CreatedAt, Is.EqualTo(assignmentBonusTime));

        Assert.That(result.RecentGains.Events[1].Description, Is.EqualTo("Made 10 tests green"));
        Assert.That(result.RecentGains.Events[1].XpEarned, Is.EqualTo(100));
        Assert.That(result.RecentGains.Events[1].CreatedAt, Is.EqualTo(latestTestTime));
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WithSinglePassedTest_ShouldUseSingularDescription()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var testTime = DateTime.UtcNow.AddMinutes(-15);
        profile.AwardPassedTestXp(42, testTime);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result.RecentGains, Is.Not.Null);
        Assert.That(result.RecentGains.TotalXpGained, Is.EqualTo(10));
        Assert.That(result.RecentGains.Events.Count, Is.EqualTo(1));
        Assert.That(result.RecentGains.Events[0].Description, Is.EqualTo("Made 1 test green"));
        Assert.That(result.RecentGains.Events[0].XpEarned, Is.EqualTo(10));
        Assert.That(result.RecentGains.Events[0].CreatedAt, Is.EqualTo(testTime));
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WhenStreakDoesNotNeedEvaluation_ShouldNotQueryCourseTestIds()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        profile.AwardPassedTestXp(101, DateTime.UtcNow.AddDays(-2));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result.CurrentWeeklyStreak, Is.EqualTo(1));
        _assignmentRepositoryMock.Verify(r => r.GetCourseTestIdsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WhenMissedLastWeekAndNotAllTestsPassed_ShouldResetCurrentStreakToZeroAndPersist()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var threeWeeksAgo = DateTime.UtcNow.AddDays(-21);
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
        profile.AwardPassedTestXp(101, threeWeeksAgo);
        profile.AwardPassedTestXp(102, twoWeeksAgo);
        profile.RecordLeaderboardVisit(DateTime.UtcNow.AddMinutes(-2));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _assignmentRepositoryMock.Setup(r => r.GetCourseTestIdsAsync(courseId, periodId))
            .ReturnsAsync(new List<int> { 101, 102, 103 });
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result.CurrentWeeklyStreak, Is.Zero);
        Assert.That(result.BestWeeklyStreak, Is.EqualTo(2));
        _assignmentRepositoryMock.Verify(r => r.GetCourseTestIdsAsync(courseId, periodId), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile), Times.Once);
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WhenMissedLastWeekButAllAvailableCourseTestsPassed_ShouldKeepStreakAndPersist()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        var threeWeeksAgo = DateTime.UtcNow.AddDays(-21);
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
        profile.AwardPassedTestXp(101, threeWeeksAgo);
        profile.AwardPassedTestXp(102, twoWeeksAgo);
        profile.RecordLeaderboardVisit(DateTime.UtcNow.AddMinutes(-2));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _assignmentRepositoryMock.Setup(r => r.GetCourseTestIdsAsync(courseId, periodId))
            .ReturnsAsync(new List<int> { 101, 102 });
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result.CurrentWeeklyStreak, Is.EqualTo(2));
        Assert.That(result.BestWeeklyStreak, Is.EqualTo(2));
        _assignmentRepositoryMock.Verify(r => r.GetCourseTestIdsAsync(courseId, periodId), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(profile), Times.Once);
    }

    [Test]
    public async Task AwardWeeklySprintBadgesAsync_ShouldEvaluateWeeklyStreaksAndBatchSaveModifiedProfiles()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int year = 2026;
        const int weekNumber = 43;
        var week41 = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);
        var week42 = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var week43 = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);

        var course = new Course { Id = courseId, Code = "CSharp" };
        _courseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { course });
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsWeeklyStreakEnabled = true,
                IsLeaderboardEnabled = true,
                IsBadgesEnabled = true
            });

        var s1Expired = new StudentCourseProfile(1, courseId, periodId);
        s1Expired.AwardPassedTestXp(101, week41);
        s1Expired.AwardPassedTestXp(102, week42);

        var s2Protected = new StudentCourseProfile(2, courseId, periodId);
        s2Protected.AwardPassedTestXp(101, week41);
        s2Protected.AwardPassedTestXp(102, week42);
        s2Protected.AwardPassedTestXp(103, week42.AddHours(1));

        var s3Active = new StudentCourseProfile(3, courseId, periodId);
        s3Active.AwardPassedTestXp(101, week43);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { s1Expired, s2Protected, s3Active });
        _studentCourseProfileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { s1Expired, s2Protected, s3Active });
        _assignmentRepositoryMock.Setup(r => r.GetCourseTestIdsAsync(courseId, periodId))
            .ReturnsAsync(new List<int> { 101, 102, 103 });

        await _service.AwardWeeklySprintBadgesAsync(year, weekNumber, periodId);

        Assert.That(s1Expired.WeeklyStreak.CurrentStreak, Is.Zero);
        Assert.That(s1Expired.WeeklyStreak.BestStreak, Is.EqualTo(2));

        Assert.That(s2Protected.WeeklyStreak.CurrentStreak, Is.EqualTo(2));
        Assert.That(s2Protected.WeeklyStreak.BestStreak, Is.EqualTo(2));
        Assert.That(s2Protected.WeeklyStreak.LastActiveWeek, Is.EqualTo(weekNumber));

        Assert.That(s3Active.WeeklyStreak.CurrentStreak, Is.EqualTo(1));

        _assignmentRepositoryMock.Verify(r => r.GetCourseTestIdsAsync(courseId, periodId), Times.Once);
        _studentCourseProfileRepositoryMock.Verify(
            r => r.UpdateRangeAsync(It.Is<IEnumerable<StudentCourseProfile>>(list =>
                list.Contains(s1Expired) && list.Contains(s2Protected) && !list.Contains(s3Active))),
            Times.Once);
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenUserIsLector_ShouldSkipGamificationProcessing()
    {
        const int lectorUserId = 99;
        var testRun = new TestRun
        {
            AssignmentId = 42,
            UserId = lectorUserId,
            CreateDateTime = DateTime.UtcNow,
            TestResults = new List<TestResult> { new TestResult { TestId = 1, Passed = true } }
        };

        _studentCourseProfileRepositoryMock.Setup(r => r.IsUserLectorAsync(lectorUserId)).ReturnsAsync(true);

        await _service.ProcessTestRunAsync(testRun);

        _assignmentRepositoryMock.Verify(r => r.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
        _studentCourseProfileRepositoryMock.Verify(r => r.GetByUserCourseAndPeriodAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _studentCourseProfileRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()), Times.Never);
    }

    [Test]
    public async Task ProcessTestRunAsync_WhenStudentOvertakesAndRisesTier_ShouldAwardOvertakeAndTierUpBonusXp()
    {
        const int courseId = 10;
        const int periodId = 1;
        const int assignmentId = 42;
        const int currentStudentId = 16;
        var baseTime = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        var testRunTime = baseTime.AddHours(2);

        var chapter = new Chapter { Id = 7, CourseId = courseId, PeriodId = periodId };
        var assignment = new Assignment
        {
            Id = assignmentId,
            TopicId = 7,
            Topic = chapter,
            Tests = new List<Test> { new Test { Id = 1 }, new Test { Id = 2 }, new Test { Id = 3 } }
        };

        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignmentId)).ReturnsAsync(assignment);
        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId)
            {
                IsGamificationEnabled = true,
                IsLeaderboardEnabled = true,
                IsBadgesEnabled = true,
                IsWeeklyStreakEnabled = true
            });

        // Create 15 students ahead with 20 XP each (Ranks 1..15 in Obsidian)
        var profiles = new List<StudentCourseProfile>();
        for (int u = 1; u <= 15; u++)
        {
            var competitor = new StudentCourseProfile(u, courseId, periodId);
            competitor.AwardPassedTestXp(1, baseTime.AddMinutes(u));
            competitor.AwardPassedTestXp(2, baseTime.AddMinutes(u)); // 20 XP
            profiles.Add(competitor);
        }

        // Current student starts at Rank 16 (Diamond) with 10 XP
        var currentStudent = new StudentCourseProfile(currentStudentId, courseId, periodId);
        currentStudent.AwardPassedTestXp(1, baseTime.AddMinutes(30)); // 10 XP
        profiles.Add(currentStudent);

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(currentStudentId, courseId, periodId))
            .ReturnsAsync(currentStudent);
        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(profiles);

        // Current student passes Test 2 and Test 3 -> +20 XP -> reaches 30 XP -> jumps from Rank 16 (Diamond) to Rank 1 (Obsidian)!
        // Surpasses 15 students (15 * 4 = 60 XP) and rises 1 tier (Diamond -> Obsidian = 5 XP)
        var testRun = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = currentStudentId,
            CreateDateTime = testRunTime,
            TestResults = new List<TestResult>
            {
                new TestResult { TestId = 2, Passed = true },
                new TestResult { TestId = 3, Passed = true }
            }
        };

        _studentCourseProfileRepositoryMock.Setup(r => r.CountAssignmentCompletionsAsync(assignmentId, courseId, periodId))
            .ReturnsAsync(15); // No quick bonus
        _assignmentRepositoryMock.Setup(r => r.GetByTopicWithTests(7))
            .ReturnsAsync(new List<Assignment> { assignment, new Assignment { Id = 99, Tests = new List<Test> { new Test { Id = 999 } } } });

        await _service.ProcessTestRunAsync(testRun);

        // 10 initial + 20 passed tests + 50 assignment bonus + 60 overtake (15 ranks * 4) + 5 tier-up (Obsidian) = 145 XP
        Assert.That(currentStudent.TotalXp, Is.EqualTo(145));
        Assert.That(currentStudent.BestHallOfFameRank, Is.EqualTo(1));
        Assert.That(currentStudent.BestHallOfFameTier, Is.EqualTo(HallOfFameTier.Obsidian));
        Assert.That(currentStudent.Events.Count(e => e.EventType == GamificationEventType.HallOfFameOvertake), Is.EqualTo(15));
        Assert.That(currentStudent.Events.Count(e => e.EventType == GamificationEventType.HallOfFameTierUp), Is.EqualTo(1));
    }

    [Test]
    public async Task GetHallOfFameLeaderboardAsync_WithEqualTotalXp_ShouldRankStudentWhoReachedXpFirstHigher()
    {
        const int courseId = 10;
        const int periodId = 1;
        var earlierTime = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
        var laterTime = new DateTime(2026, 10, 20, 11, 0, 0, DateTimeKind.Utc);

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsLeaderboardEnabled = true });

        // Student 20 reached 20 XP at 11:00
        var studentLater = new StudentCourseProfile(20, courseId, periodId)
        {
            User = new User { FirstName = "Later", LastName = "Student" }
        };
        studentLater.AwardPassedTestXp(1, earlierTime);
        studentLater.AwardPassedTestXp(2, laterTime);

        // Student 30 reached 20 XP at 09:30 (earlier!)
        var studentEarlier = new StudentCourseProfile(30, courseId, periodId)
        {
            User = new User { FirstName = "Earlier", LastName = "Student" }
        };
        studentEarlier.AwardPassedTestXp(1, earlierTime);
        studentEarlier.AwardPassedTestXp(2, earlierTime.AddMinutes(30));

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(new List<StudentCourseProfile> { studentLater, studentEarlier });

        var result = await _service.GetHallOfFameLeaderboardAsync(courseId, 20, periodId);

        Assert.That(result.Entries.Count, Is.EqualTo(2));
        Assert.That(result.Entries[0].UserId, Is.EqualTo(30));
        Assert.That(result.Entries[0].Rank, Is.EqualTo(1));
        Assert.That(result.Entries[1].UserId, Is.EqualTo(20));
        Assert.That(result.Entries[1].Rank, Is.EqualTo(2));
        Assert.That(result.NextTargetStudent, Is.Not.Null);
        Assert.That(result.NextTargetStudent.UserId, Is.EqualTo(30));
        Assert.That(result.NextTargetStudent.XpNeededToOvertake, Is.EqualTo(1));
    }

    [Test]
    public async Task GetHallOfFameLeaderboardAsync_ForStudent_ShouldReturnOnlyCurrentTierEntriesVisibleTiersAndNextTarget()
    {
        const int courseId = 10;
        const int periodId = 1;
        var baseTime = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsLeaderboardEnabled = true });

        // Create 20 students: Ranks 1..15 are Obsidian, Ranks 16..20 are Diamond
        var profiles = new List<StudentCourseProfile>();
        for (int rank = 1; rank <= 20; rank++)
        {
            var p = new StudentCourseProfile(rank, courseId, periodId)
            {
                User = new User { FirstName = $"Student{rank}", LastName = "Test" }
            };
            int testsToPass = 25 - rank;
            for (int t = 1; t <= testsToPass; t++)
            {
                p.AwardPassedTestXp(t, baseTime.AddMinutes(rank));
            }
            profiles.Add(p);
        }

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(profiles);

        // Request Hall of Fame for Student #16 (top of Diamond tier, directly behind Student #15 in Obsidian)
        var result = await _service.GetHallOfFameLeaderboardAsync(courseId, 16, periodId);

        Assert.That(result.CurrentTier.Tier, Is.EqualTo(HallOfFameTier.Diamond));
        Assert.That(result.CurrentUserRank, Is.EqualTo(16));
        // Student 16 only sees the 5 students in Diamond (ranks 16..20)
        Assert.That(result.Entries.Count, Is.EqualTo(5));
        Assert.That(result.Entries.All(e => e.Tier == HallOfFameTier.Diamond), Is.True);
        // 4 visible tiers: Silver, Gold, Diamond (current), Obsidian
        Assert.That(result.VisibleTiers.Count, Is.EqualTo(4));
        Assert.That(result.VisibleTiers.Single(t => t.IsCurrentTier).Tier, Is.EqualTo(HallOfFameTier.Diamond));
        // Next target student is Rank #15 (in Obsidian)
        Assert.That(result.NextTargetStudent, Is.Not.Null);
        Assert.That(result.NextTargetStudent.Rank, Is.EqualTo(15));
        Assert.That(result.NextTargetStudent.UserId, Is.EqualTo(15));
        Assert.That(result.NextTargetStudent.XpNeededToOvertake, Is.EqualTo(11)); // 10 XP difference + 1
    }

    [Test]
    public async Task GetHallOfFameLeaderboardAsync_ForLector_ShouldReturnAllLevelsOrSelectedStudentPerspective()
    {
        const int courseId = 10;
        const int periodId = 1;
        var baseTime = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

        _courseGamificationConfigRepositoryMock.Setup(r => r.GetByCourseIdAsync(courseId))
            .ReturnsAsync(new CourseGamificationConfig(courseId) { IsGamificationEnabled = true, IsLeaderboardEnabled = true });

        var profiles = new List<StudentCourseProfile>();
        for (int rank = 1; rank <= 20; rank++)
        {
            var p = new StudentCourseProfile(rank, courseId, periodId)
            {
                User = new User { FirstName = $"Student{rank}", LastName = "Test" }
            };
            for (int t = 1; t <= (25 - rank); t++)
            {
                p.AwardPassedTestXp(t, baseTime.AddMinutes(rank));
            }
            profiles.Add(p);
        }

        _studentCourseProfileRepositoryMock.Setup(r => r.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId))
            .ReturnsAsync(profiles);

        // 1. Lector Overview (all levels)
        var lectorOverview = await _service.GetHallOfFameLeaderboardAsync(courseId, 999, periodId, isLector: true);
        Assert.That(lectorOverview.Entries.Count, Is.EqualTo(20));
        Assert.That(lectorOverview.AllTiers.Count, Is.EqualTo(8));
        Assert.That(lectorOverview.StudentOptions.Count, Is.EqualTo(20));

        // 2. Lector filtered by Diamond tier
        var lectorDiamond = await _service.GetHallOfFameLeaderboardAsync(
            courseId, 999, periodId, isLector: true, selectedTier: HallOfFameTier.Diamond);
        Assert.That(lectorDiamond.Entries.Count, Is.EqualTo(5));
        Assert.That(lectorDiamond.CurrentTier.Tier, Is.EqualTo(HallOfFameTier.Diamond));

        // 3. Lector viewing from Student #17's perspective
        var lectorStudentPerspective = await _service.GetHallOfFameLeaderboardAsync(
            courseId, 999, periodId, isLector: true, perspectiveUserId: 17);
        Assert.That(lectorStudentPerspective.CurrentUserRank, Is.EqualTo(17));
        Assert.That(lectorStudentPerspective.CurrentTier.Tier, Is.EqualTo(HallOfFameTier.Diamond));
        Assert.That(lectorStudentPerspective.Entries.Single(e => e.IsCurrentUser).UserId, Is.EqualTo(17));
        Assert.That(lectorStudentPerspective.NextTargetStudent.UserId, Is.EqualTo(16));
    }

    [Test]
    public async Task GetStudentCourseProfileAsync_WithOvertakeAndTierUpEvents_ShouldIncludeInRecentGains()
    {
        const int userId = 5;
        const int courseId = 10;
        const int periodId = 1;
        var now = DateTime.UtcNow.AddMinutes(-5);

        var profile = new StudentCourseProfile(userId, courseId, periodId)
        {
            User = new User { FirstName = "Bob", LastName = "Builder" }
        };
        profile.EvaluateHallOfFameProgression(17, 15, now); // Surpasses 2 students (+8 XP) and enters Obsidian (+5 XP)

        _studentCourseProfileRepositoryMock.Setup(r => r.GetByUserCourseAndPeriodAsync(userId, courseId, periodId))
            .ReturnsAsync(profile);
        _studentCourseProfileRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<StudentCourseProfile>()))
            .ReturnsAsync((StudentCourseProfile p) => p);

        var result = await _service.GetStudentCourseProfileAsync(userId, courseId, periodId);

        Assert.That(result.RecentGains.TotalXpGained, Is.EqualTo(13));
        Assert.That(result.RecentGains.Events.Any(e =>
            e.Description == "Overtake bonus (surpassed 2 students in Hall of Fame)" && e.XpEarned == 8), Is.True);
        Assert.That(result.RecentGains.Events.Any(e =>
            e.Description == "Hall of Fame level up bonus (Obsidian)" && e.XpEarned == 5), Is.True);
    }
}
