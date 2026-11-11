using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Communication;
using Guts.Application.Repositories;
using Guts.Application.Services.Assessment;
using Guts.Domain.CourseAggregate;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services.Assessment;

[TestFixture]
public class PeerAssessmentReminderServiceTests
{
    private Mock<IProjectAssessmentRepository> _projectAssessmentRepositoryMock;
    private Mock<IProjectRepository> _projectRepositoryMock;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
    private Mock<IProjectTeamAssessmentService> _projectTeamAssessmentServiceMock;
    private Mock<IMessageTrackerRepository> _messageTrackerRepositoryMock;
    private Mock<IMessageTrackerFactory> _messageTrackerFactoryMock;
    private Mock<IMailSender> _mailSenderMock;
    private Mock<IMailThrottler> _mailThrottlerMock;
    private Mock<ILogger<PeerAssessmentReminderService>> _loggerMock;

    private PeerAssessmentReminderService _service;
    private DateTime _executionTime;

    [SetUp]
    public void SetUp()
    {
        _projectAssessmentRepositoryMock = new Mock<IProjectAssessmentRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
        _projectTeamAssessmentServiceMock = new Mock<IProjectTeamAssessmentService>();
        _messageTrackerRepositoryMock = new Mock<IMessageTrackerRepository>();
        _messageTrackerFactoryMock = new Mock<IMessageTrackerFactory>();
        _mailSenderMock = new Mock<IMailSender>();
        _mailThrottlerMock = new Mock<IMailThrottler>();
        _loggerMock = new Mock<ILogger<PeerAssessmentReminderService>>();

        _executionTime = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

        _service = new PeerAssessmentReminderService(
            _projectAssessmentRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _projectTeamRepositoryMock.Object,
            _projectTeamAssessmentServiceMock.Object,
            _messageTrackerRepositoryMock.Object,
            _messageTrackerFactoryMock.Object,
            _mailSenderMock.Object,
            _mailThrottlerMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenNoOpenAssessments_ShouldReturnZero()
    {
        // Arrange
        _projectAssessmentRepositoryMock
            .Setup(r => r.GetOpenAssessmentsAsync(_executionTime))
            .ReturnsAsync(new List<IProjectAssessment>());

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _projectRepositoryMock.Verify(r => r.LoadWithCourseAsync(It.IsAny<int>()), Times.Never);
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectAssessment>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenAssessmentNotEligiblePerPolicy_ShouldSkip()
    {
        // Arrange: total duration is only 24 hours (< 48h)
        var assessmentMock = new Mock<IProjectAssessment>();
        assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(_executionTime.AddHours(-12));
        assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(_executionTime.AddHours(12));

        _projectAssessmentRepositoryMock
            .Setup(r => r.GetOpenAssessmentsAsync(_executionTime))
            .ReturnsAsync(new List<IProjectAssessment> { assessmentMock.Object });

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _projectRepositoryMock.Verify(r => r.LoadWithCourseAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenEligible_NewStudentNeedingReminder_ShouldCreateTrackerSendEmailMarkSucceededAndThrottle()
    {
        // Arrange: 72h window, 12h remaining
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "DotNet Advanced", "proj-code", "Awesome Project");
        var student = new User { Id = 1, FirstName = "Alice", Email = "alice@pxl.be" };
        var teamMock = CreateTeamMock(20, student);
        var teamAssessmentMock = CreateTeamAssessmentMock(student);

        _projectAssessmentRepositoryMock
            .Setup(r => r.GetOpenAssessmentsAsync(_executionTime))
            .ReturnsAsync(new List<IProjectAssessment> { assessmentMock.Object });

        _projectRepositoryMock
            .Setup(r => r.LoadWithCourseAsync(100))
            .ReturnsAsync(projectMock.Object);

        _projectTeamRepositoryMock
            .Setup(r => r.GetByProjectWithUsersAsync(100))
            .ReturnsAsync(new List<IProjectTeam> { teamMock.Object });

        _projectTeamAssessmentServiceMock
            .Setup(s => s.GetOrCreateTeamAssessmentAsync(10, 20))
            .ReturnsAsync(teamAssessmentMock.Object);

        _messageTrackerRepositoryMock
            .Setup(r => r.FindByEntityAndEmailAsync("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email)))
            .ReturnsAsync((IMessageTracker)null);

        var newTrackerMock = new Mock<IMessageTracker>();
        newTrackerMock.SetupGet(t => t.Status).Returns(MessageStatus.Sending);

        _messageTrackerFactoryMock
            .Setup(f => f.CreateNew("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email), _executionTime))
            .Returns(newTrackerMock.Object);

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(1));

        _messageTrackerRepositoryMock.Verify(r => r.AddAsync(newTrackerMock.Object), Times.Once);
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            student,
            "DotNet Advanced",
            5,
            "proj-code",
            "Awesome Project",
            assessmentMock.Object,
            20), Times.Once);

        newTrackerMock.Verify(t => t.MarkSucceeded(), Times.Once);
        _messageTrackerRepositoryMock.Verify(r => r.UpdateAsync(newTrackerMock.Object), Times.Once);
        _mailThrottlerMock.Verify(t => t.ThrottleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenTrackerAlreadySucceeded_ShouldSkipStudent()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "DotNet Advanced", "proj-code", "Awesome Project");
        var student = new User { Id = 1, FirstName = "Alice", Email = "alice@pxl.be" };
        var teamMock = CreateTeamMock(20, student);
        var teamAssessmentMock = CreateTeamAssessmentMock(student);

        SetupStandardMocks(assessmentMock, projectMock, teamMock, teamAssessmentMock);

        var existingTrackerMock = new Mock<IMessageTracker>();
        existingTrackerMock.SetupGet(t => t.Status).Returns(MessageStatus.Succeeded);

        _messageTrackerRepositoryMock
            .Setup(r => r.FindByEntityAndEmailAsync("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email)))
            .ReturnsAsync(existingTrackerMock.Object);

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectAssessment>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenTrackerFailedWithMaxRetries_ShouldSkipStudent()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "DotNet Advanced", "proj-code", "Awesome Project");
        var student = new User { Id = 1, FirstName = "Alice", Email = "alice@pxl.be" };
        var teamMock = CreateTeamMock(20, student);
        var teamAssessmentMock = CreateTeamAssessmentMock(student);

        SetupStandardMocks(assessmentMock, projectMock, teamMock, teamAssessmentMock);

        var existingTrackerMock = new Mock<IMessageTracker>();
        existingTrackerMock.SetupGet(t => t.Status).Returns(MessageStatus.Failed);
        existingTrackerMock.SetupGet(t => t.RetryCount).Returns(3);
        existingTrackerMock.SetupGet(t => t.CanRetry).Returns(false);

        _messageTrackerRepositoryMock
            .Setup(r => r.FindByEntityAndEmailAsync("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email)))
            .ReturnsAsync(existingTrackerMock.Object);

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectAssessment>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenTrackerFailedAndCanRetry_ShouldRetryTransitionToSendingSendMailAndMarkSucceeded()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "DotNet Advanced", "proj-code", "Awesome Project");
        var student = new User { Id = 1, FirstName = "Alice", Email = "alice@pxl.be" };
        var teamMock = CreateTeamMock(20, student);
        var teamAssessmentMock = CreateTeamAssessmentMock(student);

        SetupStandardMocks(assessmentMock, projectMock, teamMock, teamAssessmentMock);

        var existingTrackerMock = new Mock<IMessageTracker>();
        existingTrackerMock.SetupGet(t => t.Status).Returns(MessageStatus.Failed);
        existingTrackerMock.SetupGet(t => t.RetryCount).Returns(1);
        existingTrackerMock.SetupGet(t => t.CanRetry).Returns(true);

        _messageTrackerRepositoryMock
            .Setup(r => r.FindByEntityAndEmailAsync("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email)))
            .ReturnsAsync(existingTrackerMock.Object);

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(1));
        existingTrackerMock.Verify(t => t.Retry(_executionTime), Times.Once);
        _messageTrackerRepositoryMock.Verify(r => r.UpdateAsync(existingTrackerMock.Object), Times.Exactly(2));
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            student, "DotNet Advanced", 5, "proj-code", "Awesome Project", assessmentMock.Object, 20), Times.Once);
        existingTrackerMock.Verify(t => t.MarkSucceeded(), Times.Once);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenMailSendingFails_ShouldMarkTrackerFailedPersistAndContinue()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "DotNet Advanced", "proj-code", "Awesome Project");
        var student = new User { Id = 1, FirstName = "Alice", Email = "alice@pxl.be" };
        var teamMock = CreateTeamMock(20, student);
        var teamAssessmentMock = CreateTeamAssessmentMock(student);

        SetupStandardMocks(assessmentMock, projectMock, teamMock, teamAssessmentMock);

        var newTrackerMock = new Mock<IMessageTracker>();
        newTrackerMock.SetupGet(t => t.Status).Returns(MessageStatus.Sending);

        _messageTrackerRepositoryMock
            .Setup(r => r.FindByEntityAndEmailAsync("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email)))
            .ReturnsAsync((IMessageTracker)null);

        _messageTrackerFactoryMock
            .Setup(f => f.CreateNew("PeerAssessment", 10, It.Is<Email>(e => e.Value == student.Email), _executionTime))
            .Returns(newTrackerMock.Object);

        _mailSenderMock
            .Setup(m => m.SendPeerAssessmentReminderEmailAsync(
                student, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectAssessment>(), It.IsAny<int>()))
            .ThrowsAsync(new SmtpException("Network unreachable"));

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        newTrackerMock.Verify(t => t.MarkFailed(It.Is<string>(msg => msg.Contains("Network unreachable"))), Times.Once);
        _messageTrackerRepositoryMock.Verify(r => r.UpdateAsync(newTrackerMock.Object), Times.Once);
        newTrackerMock.Verify(t => t.MarkSucceeded(), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenTeamHasNoMembers_ShouldSkipTeam()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "Course", "code", "Project");
        var emptyTeamMock = new Mock<IProjectTeam>();
        emptyTeamMock.SetupGet(t => t.Id).Returns(25);
        emptyTeamMock.SetupGet(t => t.TeamUsers).Returns(new List<IProjectTeamUser>());

        _projectAssessmentRepositoryMock
            .Setup(r => r.GetOpenAssessmentsAsync(_executionTime))
            .ReturnsAsync(new List<IProjectAssessment> { assessmentMock.Object });

        _projectRepositoryMock
            .Setup(r => r.LoadWithCourseAsync(100))
            .ReturnsAsync(projectMock.Object);

        _projectTeamRepositoryMock
            .Setup(r => r.GetByProjectWithUsersAsync(100))
            .ReturnsAsync(new List<IProjectTeam> { emptyTeamMock.Object });

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _projectTeamAssessmentServiceMock.Verify(s => s.GetOrCreateTeamAssessmentAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SendAssessmentRemindersAsync_WhenStudentEmailIsNullOrEmpty_ShouldSkipStudent()
    {
        // Arrange
        var assessmentMock = CreateEligibleAssessmentMock(10, 100);
        var projectMock = CreateProjectMock(100, 5, "Course", "code", "Project");
        var studentWithoutEmail = new User { Id = 1, FirstName = "Alice", Email = null };
        var teamMock = CreateTeamMock(20, studentWithoutEmail);
        var teamAssessmentMock = CreateTeamAssessmentMock(studentWithoutEmail);

        SetupStandardMocks(assessmentMock, projectMock, teamMock, teamAssessmentMock);

        // Act
        int sent = await _service.SendAssessmentRemindersAsync(_executionTime);

        // Assert
        Assert.That(sent, Is.EqualTo(0));
        _messageTrackerRepositoryMock.Verify(r => r.FindByEntityAndEmailAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Email>()), Times.Never);
        _mailSenderMock.Verify(m => m.SendPeerAssessmentReminderEmailAsync(
            It.IsAny<User>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectAssessment>(), It.IsAny<int>()), Times.Never);
    }

    private Mock<IProjectAssessment> CreateEligibleAssessmentMock(int assessmentId, int projectId)
    {
        var assessmentMock = new Mock<IProjectAssessment>();
        assessmentMock.SetupGet(a => a.Id).Returns(assessmentId);
        assessmentMock.SetupGet(a => a.ProjectId).Returns(projectId);
        assessmentMock.SetupGet(a => a.Description).Returns("Sprint Review");
        assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(_executionTime.AddHours(-60));
        assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(_executionTime.AddHours(12));
        return assessmentMock;
    }

    private Mock<IProject> CreateProjectMock(int projectId, int courseId, string courseName, string projectCode, string projectDescription)
    {
        var projectMock = new Mock<IProject>();
        projectMock.SetupGet(p => p.Id).Returns(projectId);
        projectMock.SetupGet(p => p.CourseId).Returns(courseId);
        projectMock.SetupGet(p => p.Code).Returns(new Code(projectCode));
        projectMock.SetupGet(p => p.Description).Returns(projectDescription);

        var course = new Course { Id = courseId, Name = courseName, Code = "CRS" };
        projectMock.SetupGet(p => p.Course).Returns(course);
        return projectMock;
    }

    private static Mock<IProjectTeam> CreateTeamMock(int teamId, User user)
    {
        var teamMock = new Mock<IProjectTeam>();
        teamMock.SetupGet(t => t.Id).Returns(teamId);

        var teamUserMock = new Mock<IProjectTeamUser>();
        teamUserMock.SetupGet(tu => tu.UserId).Returns(user.Id);
        teamUserMock.SetupGet(tu => tu.User).Returns(user);

        teamMock.SetupGet(t => t.TeamUsers).Returns(new List<IProjectTeamUser> { teamUserMock.Object });
        return teamMock;
    }

    private static Mock<IProjectTeamAssessment> CreateTeamAssessmentMock(User pendingPeer)
    {
        var teamAssessmentMock = new Mock<IProjectTeamAssessment>();
        teamAssessmentMock.Setup(ta => ta.GetPeersThatNeedToEvaluateOthers()).Returns(new List<User> { pendingPeer });
        return teamAssessmentMock;
    }

    private void SetupStandardMocks(
        Mock<IProjectAssessment> assessmentMock,
        Mock<IProject> projectMock,
        Mock<IProjectTeam> teamMock,
        Mock<IProjectTeamAssessment> teamAssessmentMock)
    {
        _projectAssessmentRepositoryMock
            .Setup(r => r.GetOpenAssessmentsAsync(_executionTime))
            .ReturnsAsync(new List<IProjectAssessment> { assessmentMock.Object });

        _projectRepositoryMock
            .Setup(r => r.LoadWithCourseAsync(assessmentMock.Object.ProjectId))
            .ReturnsAsync(projectMock.Object);

        _projectTeamRepositoryMock
            .Setup(r => r.GetByProjectWithUsersAsync(assessmentMock.Object.ProjectId))
            .ReturnsAsync(new List<IProjectTeam> { teamMock.Object });

        _projectTeamAssessmentServiceMock
            .Setup(s => s.GetOrCreateTeamAssessmentAsync(assessmentMock.Object.Id, teamMock.Object.Id))
            .ReturnsAsync(teamAssessmentMock.Object);
    }
}
