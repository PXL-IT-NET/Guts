using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Communication;
using Guts.Application.Dtos.Gamification;
using Guts.Application.Repositories;
using Guts.Application.Services;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class StreakReminderServiceTests
{
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<ICourseRepository> _courseRepositoryMock;
    private Mock<ICourseGamificationConfigRepository> _configRepositoryMock;
    private Mock<ITopicRepository> _topicRepositoryMock;
    private Mock<IStudentCourseProfileRepository> _profileRepositoryMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IMailSender> _mailSenderMock;
    private Mock<ILogger<StreakReminderService>> _loggerMock;
    private StreakReminderService _service;

    private IPeriod _period;
    private Course _course;
    private CourseGamificationConfig _config;
    private List<ITopic> _topics;
    private DateTime _thursdayExecution;

    [SetUp]
    public void Setup()
    {
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _configRepositoryMock = new Mock<ICourseGamificationConfigRepository>();
        _topicRepositoryMock = new Mock<ITopicRepository>();
        _profileRepositoryMock = new Mock<IStudentCourseProfileRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _mailSenderMock = new Mock<IMailSender>();
        _loggerMock = new Mock<ILogger<StreakReminderService>>();

        _service = new StreakReminderService(
            _periodRepositoryMock.Object,
            _courseRepositoryMock.Object,
            _configRepositoryMock.Object,
            _topicRepositoryMock.Object,
            _profileRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mailSenderMock.Object,
            _loggerMock.Object,
            TimeSpan.Zero); // zero throttle delay for tests

        _period = Mock.Of<IPeriod>(p => p.Id == 1);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(_period);

        _course = new Course { Id = 10, Code = "OOP", Name = "Object-Oriented Programming" };
        _courseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Course>)[_course]);

        _config = new CourseGamificationConfig(10)
        {
            IsGamificationEnabled = true,
            IsWeeklyStreakEnabled = true
        };
        _configRepositoryMock.Setup(r => r.GetByCourseIdAsync(10)).ReturnsAsync(_config);

        // Execution time: Thursday 2026-10-29 12:00 UTC (ISO week 44)
        _thursdayExecution = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        var assignment1 = new Assignment
        {
            Id = 101,
            Code = "Ex1",
            Description = "Exercise 1",
            Tests = (List<Test>)
            [
                new() { Id = 1001 },
                new() { Id = 1002 }
            ]
        };
        var assignment2 = new Assignment
        {
            Id = 102,
            Code = "Ex2",
            Description = "Exercise 2",
            Tests = (List<Test>)
            [
                new() { Id = 1003 },
                new() { Id = 1004 }
            ]
        };

        var topicMock = new Mock<ITopic>();
        topicMock.SetupGet(t => t.Id).Returns(1);
        topicMock.SetupGet(t => t.Description).Returns("Chapter 1: Basics");
        topicMock.SetupGet(t => t.Assignments).Returns((List<Assignment>)[assignment1, assignment2]);
        _topics = [topicMock.Object];

        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync(_topics);

        _profileRepositoryMock.Setup(r => r.IsUserLectorAsync(It.IsAny<int>())).ReturnsAsync(false);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenStudentHasStreakAndNoXpThisWeek_ShouldQueueAndSendReminder()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var studentProfile = new StudentCourseProfile(42, 10, 1);
        studentProfile.RecordWeeklyActivity(previousWeekDate); // sets CurrentStreak = 1
        studentProfile.AwardPassedTestXp(1001, previousWeekDate); // passed 1 of 4 tests in course

        var user = new User { Id = 42, FirstName = "Alice", Email = "alice@test.com" };
        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(1));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.Is<User>(u => u.Id == 42 && u.Email == "alice@test.com"),
            "Object-Oriented Programming",
            10,
            1,
            It.IsAny<string>(),
            It.Is<IReadOnlyList<StreakReminderAssignmentDto>>(list => list.Count == 2)), Times.Once);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenStudentEarnedXpThisWeek_ShouldNotSendReminder()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var studentProfile = new StudentCourseProfile(42, 10, 1);
        studentProfile.RecordWeeklyActivity(previousWeekDate);
        studentProfile.AwardPassedTestXp(1001, _thursdayExecution.AddHours(-2)); // earned XP in current week

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()), Times.Never);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenStudentHasZeroStreak_ShouldNotSendReminder()
    {
        // Arrange
        var studentProfile = new StudentCourseProfile(42, 10, 1); // CurrentStreak = 0

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()), Times.Never);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenStudentCompletedAllTests_ShouldNotSendReminder()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var studentProfile = new StudentCourseProfile(42, 10, 1);
        studentProfile.RecordWeeklyActivity(previousWeekDate);
        // Student completed all 4 tests in previous week
        studentProfile.AwardPassedTestXp(1001, previousWeekDate);
        studentProfile.AwardPassedTestXp(1002, previousWeekDate);
        studentProfile.AwardPassedTestXp(1003, previousWeekDate);
        studentProfile.AwardPassedTestXp(1004, previousWeekDate);

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()), Times.Never);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenUserIsLector_ShouldNotSendReminder()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var studentProfile = new StudentCourseProfile(42, 10, 1);
        studentProfile.RecordWeeklyActivity(previousWeekDate);

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);
        _profileRepositoryMock.Setup(r => r.IsUserLectorAsync(42)).ReturnsAsync(true);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()), Times.Never);
    }

    [Test]
    public async Task SendStreakRemindersAsync_ShouldRecommendInProgressAssignmentsBeforeUntouchedAssignments()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var studentProfile = new StudentCourseProfile(42, 10, 1);
        studentProfile.RecordWeeklyActivity(previousWeekDate);
        // Passed test 1003 on assignment2 (Id 102), but not test 1004 -> in progress!
        // Assignment1 (Id 101) has 0 tests passed -> untouched!
        studentProfile.AwardPassedTestXp(1003, previousWeekDate);

        var user = new User { Id = 42, FirstName = "Alice", Email = "alice@test.com" };
        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[studentProfile]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(user);

        IReadOnlyList<StreakReminderAssignmentDto> capturedRecommendations = null;
        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Callback<User, string, int, int, string, IReadOnlyList<StreakReminderAssignmentDto>>((u, cName, cId, s, target, recs) => capturedRecommendations = recs)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(capturedRecommendations, Is.Not.Null);
        Assert.That(capturedRecommendations.Count, Is.EqualTo(2));
        // First recommendation must be the in-progress assignment (102)
        Assert.That(capturedRecommendations[0].AssignmentId, Is.EqualTo(102));
        Assert.That(capturedRecommendations[0].GreenTestsCount, Is.EqualTo(1));
        // Second recommendation must be the untouched assignment (101)
        Assert.That(capturedRecommendations[1].AssignmentId, Is.EqualTo(101));
        Assert.That(capturedRecommendations[1].GreenTestsCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenStudentOptedIn_ShouldIncludeNextTargetStudent()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var bob = new StudentCourseProfile(1, 10, 1);
        bob.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(1));
        bob.AwardPassedTestXp(1002, previousWeekDate.AddMinutes(2)); // Bob has 20 XP
        var bobUser = new User { Id = 1, FirstName = "Bob", LastName = "Builder" };

        var alice = new StudentCourseProfile(2, 10, 1);
        alice.RecordWeeklyActivity(previousWeekDate);
        alice.AwardPassedTestXp(1003, previousWeekDate.AddMinutes(3)); // Alice has 10 XP
        var aliceUser = new User { Id = 2, FirstName = "Alice", LastName = "Wonderland", Email = "alice@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[bob, alice]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bobUser);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(aliceUser);

        string capturedTargetStudent = null;
        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Callback<User, string, int, int, string, IReadOnlyList<StreakReminderAssignmentDto>>((u, cName, cId, s, target, recs) => capturedTargetStudent = target)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(capturedTargetStudent, Is.EqualTo("Bob Builder"));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenEmailFailsForOneStudent_ShouldLogAndContinueQueue()
    {
        // Arrange
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student1 = new StudentCourseProfile(1, 10, 1);
        student1.RecordWeeklyActivity(previousWeekDate);
        var user1 = new User { Id = 1, Email = "student1@test.com" };

        var student2 = new StudentCourseProfile(2, 10, 1);
        student2.RecordWeeklyActivity(previousWeekDate);
        var user2 = new User { Id = 2, Email = "student2@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student1, student2]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user2);

        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.Is<User>(u => u.Id == 1), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .ThrowsAsync(new InvalidOperationException("SMTP connection dropped"));

        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.Is<User>(u => u.Id == 2), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Returns(Task.CompletedTask);

        // Act
        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Assert
        Assert.That(sent, Is.EqualTo(1));
        _mailSenderMock.Verify(m => m.SendStreakReminderEmailAsync(
            It.Is<User>(u => u.Id == 2), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()), Times.Once);
    }

    [Test]
    public void Constructors_ShouldInitializeThrottleDelay()
    {
        var serviceDefault = new StreakReminderService(
            _periodRepositoryMock.Object,
            _courseRepositoryMock.Object,
            _configRepositoryMock.Object,
            _topicRepositoryMock.Object,
            _profileRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mailSenderMock.Object,
            _loggerMock.Object);

        Assert.That(serviceDefault.ThrottleDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));

        serviceDefault.ThrottleDelay = TimeSpan.FromSeconds(5);
        Assert.That(serviceDefault.ThrottleDelay, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    [TestCase(false, true, "Config null")]
    [TestCase(true, false, "Gamification disabled")]
    [TestCase(true, true, "Weekly streak disabled")]
    public async Task SendStreakRemindersAsync_WhenGamificationOrStreakDisabled_ShouldReturnZero(
        bool hasConfig, bool isGamificationEnabled, string scenario)
    {
        if (!hasConfig)
        {
            _configRepositoryMock.Setup(r => r.GetByCourseIdAsync(10)).ReturnsAsync((CourseGamificationConfig)null);
        }
        else if (!isGamificationEnabled)
        {
            _config.IsGamificationEnabled = false;
        }
        else
        {
            _config.IsWeeklyStreakEnabled = false;
        }

        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(sent, Is.EqualTo(0));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenTopicsNullOrEmptyOrNoTests_ShouldReturnZero()
    {
        // Topics null
        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync((List<ITopic>)null);
        Assert.That(await _service.SendStreakRemindersAsync(_thursdayExecution), Is.EqualTo(0));

        // Topics empty
        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync((List<ITopic>)[]);
        Assert.That(await _service.SendStreakRemindersAsync(_thursdayExecution), Is.EqualTo(0));

        // Topic with no tests
        var emptyTopic = Mock.Of<ITopic>(t => t.Assignments == new List<Assignment> { new() { Tests = new List<Test>() } });
        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync((List<ITopic>)[emptyTopic]);
        Assert.That(await _service.SendStreakRemindersAsync(_thursdayExecution), Is.EqualTo(0));

        // Topic with null assignments
        var nullAssignmentsTopic = Mock.Of<ITopic>(t => t.Assignments == null);
        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync((List<ITopic>)[nullAssignmentsTopic]);
        Assert.That(await _service.SendStreakRemindersAsync(_thursdayExecution), Is.EqualTo(0));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenUserHasNoEmailOrUserNull_ShouldSkipProfile()
    {
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student1 = new StudentCourseProfile(1, 10, 1);
        student1.RecordWeeklyActivity(previousWeekDate);
        var userNoEmail = new User { Id = 1, Email = "   " };

        var student2 = new StudentCourseProfile(2, 10, 1);
        student2.RecordWeeklyActivity(previousWeekDate);

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student1, student2]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(userNoEmail);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((User)null);

        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(sent, Is.EqualTo(0));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenCourseNameNull_ShouldFallbackToCourseCode()
    {
        _course.Name = null;
        _course.Code = "CS101";

        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student = new StudentCourseProfile(42, 10, 1);
        student.RecordWeeklyActivity(previousWeekDate);
        var user = new User { Id = 42, Email = "test@test.com" };
        student.User = user; // profile.User already populated

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student]);

        string capturedCourseName = null;
        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Callback<User, string, int, int, string, IReadOnlyList<StreakReminderAssignmentDto>>((u, cName, cId, s, target, recs) => capturedCourseName = cName)
            .Returns(Task.CompletedTask);

        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(sent, Is.EqualTo(1));
        Assert.That(capturedCourseName, Is.EqualTo("CS101"));
        _userRepositoryMock.Verify(r => r.GetByIdAsync(42), Times.Never);
    }

    [Test]
    public async Task SendStreakRemindersAsync_TargetStudentNameResolution_ShouldCoverAllBranches()
    {
        var previousWeekDate = _thursdayExecution.AddDays(-7);

        // Leaderboard:
        // Rank 1: Bob (Pseudonym "ShadowBob", UsePseudonym = true, 30 XP)
        var bob = new StudentCourseProfile(1, 10, 1);
        bob.Enroll(true, "ShadowBob", "CodeCat", null, true);
        bob.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(1));
        bob.AwardPassedTestXp(1002, previousWeekDate.AddMinutes(2));
        bob.AwardPassedTestXp(1003, previousWeekDate.AddMinutes(3));

        // Rank 2: Charlie (UsePseudonym = false, User with names, 20 XP)
        var charlie = new StudentCourseProfile(2, 10, 1);
        charlie.Enroll(true, null, "CodeCat", null, false);
        charlie.User = new User { Id = 2, FirstName = "Charlie", LastName = "Chaplin" };
        charlie.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(4));
        charlie.AwardPassedTestXp(1002, previousWeekDate.AddMinutes(5));

        // Rank 3: Diana (UsePseudonym = false, User loaded via repo with empty names, fallback to pseudonym, 15 XP)
        var diana = new StudentCourseProfile(3, 10, 1);
        diana.Enroll(true, "DianaSecret", "CodeCat", null, false);
        diana.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(6));

        // Rank 4: Alice (needs reminder, ShowInLeaderBoard = true, 10 XP)
        var alice = new StudentCourseProfile(4, 10, 1);
        alice.RecordWeeklyActivity(previousWeekDate);
        alice.User = new User { Id = 4, Email = "alice@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[bob, charlie, diana, alice]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new User { Id = 3, FirstName = " ", LastName = " " });

        string capturedTarget = null;
        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Callback<User, string, int, int, string, IReadOnlyList<StreakReminderAssignmentDto>>((u, cName, cId, s, target, recs) => capturedTarget = target)
            .Returns(Task.CompletedTask);

        await _service.SendStreakRemindersAsync(_thursdayExecution);

        // Diana is rank 3 directly above Alice (rank 4). Since Diana's user names are whitespace, falls back to Pseudonym "DianaSecret"
        Assert.That(capturedTarget, Is.EqualTo("DianaSecret"));

        // Case: student opted out of leaderboard
        alice.HideFromLeaderboard();
        capturedTarget = "initial";
        await _service.SendStreakRemindersAsync(_thursdayExecution);
        Assert.That(capturedTarget, Is.Null);

        // Case: student is #1 on leaderboard
        bob.RecordWeeklyActivity(previousWeekDate);
        bob.User = new User { Id = 1, Email = "bob@test.com" };
        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[bob]);
        capturedTarget = "initial";
        await _service.SendStreakRemindersAsync(_thursdayExecution);
        Assert.That(capturedTarget, Is.Null);
    }

    [Test]
    public async Task SendStreakRemindersAsync_WithMultipleMessagesAndThrottleDelay_ShouldDelayBetweenMessages()
    {
        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student1 = new StudentCourseProfile(1, 10, 1);
        student1.RecordWeeklyActivity(previousWeekDate);
        student1.User = new User { Id = 1, Email = "s1@test.com" };

        var student2 = new StudentCourseProfile(2, 10, 1);
        student2.RecordWeeklyActivity(previousWeekDate);
        student2.User = new User { Id = 2, Email = "s2@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student1, student2]);

        _service.ThrottleDelay = TimeSpan.FromMilliseconds(5);

        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(sent, Is.EqualTo(2));
    }

    [Test]
    public void SendStreakRemindersAsync_WhenCancellationTokenCancelled_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student = new StudentCourseProfile(1, 10, 1);
        student.RecordWeeklyActivity(previousWeekDate);
        student.User = new User { Id = 1, Email = "s1@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student]);

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.SendStreakRemindersAsync(_thursdayExecution, cts.Token));
    }

    [Test]
    public async Task SendStreakRemindersAsync_WhenAssignmentHasNoTestsOrAllTestsPassed_ShouldHandleInRecommendations()
    {
        var assignmentEmptyTests = new Assignment
        {
            Id = 103,
            Code = "Ex3",
            Description = "Empty Tests"
        };
        assignmentEmptyTests.LoadTests((List<Test>)[]);

        var assignmentNullTests = new Assignment
        {
            Id = 104,
            Code = "Ex4",
            Description = "Null Tests"
        };
        assignmentNullTests.ClearTests();

        var assignmentCompleted = new Assignment
        {
            Id = 105,
            Code = "Ex5",
            Description = "Completed"
        };
        assignmentCompleted.AddTest("Test1", id: 1005);

        var topicMockWithNullAssignments = new Mock<ITopic>();
        topicMockWithNullAssignments.SetupGet(t => t.Id).Returns(3);
        topicMockWithNullAssignments.SetupGet(t => t.Assignments).Returns((ICollection<Assignment>)null);

        var topicMock = new Mock<ITopic>();
        topicMock.SetupGet(t => t.Id).Returns(2);
        topicMock.SetupGet(t => t.Assignments).Returns((List<Assignment>)
            [assignmentEmptyTests, assignmentNullTests, assignmentCompleted, _topics[0].Assignments.First()]);
        _topicRepositoryMock.Setup(r => r.GetByCourseWithAssignmentsAndTestsAsync(10, 1))
            .ReturnsAsync((List<ITopic>)[topicMockWithNullAssignments.Object, topicMock.Object]);

        var previousWeekDate = _thursdayExecution.AddDays(-7);
        var student = new StudentCourseProfile(1, 10, 1);
        student.RecordWeeklyActivity(previousWeekDate);
        // Add an event from a different year to test the ISOWeek.GetYear false branch
        student.AwardPassedTestXp(1001, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        student.AwardPassedTestXp(1005, previousWeekDate); // completed assignment 105
        student.User = new User { Id = 1, Email = "s@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[student]);

        int sent = await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(sent, Is.EqualTo(1));
    }

    [Test]
    public async Task SendStreakRemindersAsync_TargetStudentNameWithWhitespacePseudonymOrTargetUserResolution()
    {
        var previousWeekDate = _thursdayExecution.AddDays(-7);

        // Target with UsePseudonym = false -> uses user names
        var target1 = new StudentCourseProfile(10, 10, 1);
        target1.Enroll(true, null, "CodeCat", null, false);
        target1.User = new User { Id = 10, FirstName = "Real", LastName = "User" };
        target1.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(1));
        target1.AwardPassedTestXp(1002, previousWeekDate.AddMinutes(2));

        var student1 = new StudentCourseProfile(11, 10, 1);
        student1.RecordWeeklyActivity(previousWeekDate);
        student1.User = new User { Id = 11, Email = "s1@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[target1, student1]);

        string capturedTarget = null;
        _mailSenderMock.Setup(m => m.SendStreakReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StreakReminderAssignmentDto>>()))
            .Callback<User, string, int, int, string, IReadOnlyList<StreakReminderAssignmentDto>>((u, cName, cId, s, target, recs) => capturedTarget = target)
            .Returns(Task.CompletedTask);

        await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(capturedTarget, Is.EqualTo("Real User"));

        // Target user retrieved from repository has real name
        var target2 = new StudentCourseProfile(20, 10, 1);
        target2.Enroll(true, null, "CodeCat", null, false);
        target2.AwardPassedTestXp(1001, previousWeekDate.AddMinutes(1));
        target2.AwardPassedTestXp(1002, previousWeekDate.AddMinutes(2));

        var student2 = new StudentCourseProfile(21, 10, 1);
        student2.RecordWeeklyActivity(previousWeekDate);
        student2.User = new User { Id = 21, Email = "s2@test.com" };

        _profileRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, 1))
            .ReturnsAsync((List<StudentCourseProfile>)[target2, student2]);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new User { Id = 20, FirstName = "Repo", LastName = "User" });

        capturedTarget = null;
        await _service.SendStreakRemindersAsync(_thursdayExecution);

        Assert.That(capturedTarget, Is.EqualTo("Repo User"));
    }
}

