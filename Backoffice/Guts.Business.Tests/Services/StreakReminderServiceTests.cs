using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services;

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
        _courseRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { _course });

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
            Tests = new List<Test>
            {
                new() { Id = 1001 },
                new() { Id = 1002 }
            }
        };
        var assignment2 = new Assignment
        {
            Id = 102,
            Code = "Ex2",
            Description = "Exercise 2",
            Tests = new List<Test>
            {
                new() { Id = 1003 },
                new() { Id = 1004 }
            }
        };

        var topicMock = new Mock<ITopic>();
        topicMock.SetupGet(t => t.Id).Returns(1);
        topicMock.SetupGet(t => t.Description).Returns("Chapter 1: Basics");
        topicMock.SetupGet(t => t.Assignments).Returns(new List<Assignment> { assignment1, assignment2 });
        _topics = new List<ITopic> { topicMock.Object };

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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });
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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });

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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });

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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });

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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });
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
            .ReturnsAsync(new List<StudentCourseProfile> { studentProfile });
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
            .ReturnsAsync(new List<StudentCourseProfile> { bob, alice });
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
            .ReturnsAsync(new List<StudentCourseProfile> { student1, student2 });
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
}

