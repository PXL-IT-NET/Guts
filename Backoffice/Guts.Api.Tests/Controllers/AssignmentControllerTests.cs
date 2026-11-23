using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Controllers;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services;
using Guts.Application.Tests.Builders;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class AssignmentControllerTests
{
    private AssignmentController _controller;
    private Random _random;
    private Mock<IAssignmentService> _assignmentServiceMock;
    private Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private Mock<IAssignmentConverter> _assignmentConverterMock;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
    private Mock<ITopicService> _topicServiceMock;
    private Mock<IMapper> _mapperMock;
    private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;
    private Mock<ISolutionFileService> _solutionFileServiceMock;

    [SetUp]
    public void Setup()
    {
        _random = new();
        _assignmentServiceMock = new();
        _assignmentRepositoryMock = new();
        _assignmentConverterMock = new();
        _projectTeamRepositoryMock = new();
        _topicServiceMock = new();
        _solutionFileRepositoryMock = new();
        _solutionFileServiceMock = new();
        _mapperMock = new();

        _controller = new(_assignmentServiceMock.Object,
            _assignmentRepositoryMock.Object, 
            _assignmentConverterMock.Object,
            _projectTeamRepositoryMock.Object, 
            _topicServiceMock.Object,
            _solutionFileRepositoryMock.Object,
            _solutionFileServiceMock.Object,
            _mapperMock.Object);
    }

    [Test]
    public void ShouldInheritFromControllerBase()
    {
        Assert.That(_controller, Is.InstanceOf<ControllerBase>());
    }

    [Test]
    public void GetAssignmentResultsForUser_ShouldReturnAssignmentDetailsIfParametersAreValid()
    {
        //Arrange
        var existingAssignment = new AssignmentBuilder().WithId().Build();
        var userId = _random.NextPositive();

        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()))
            .ReturnsAsync(existingAssignment);

        var assignmentResultDto = new AssignmentResultDto();
        _assignmentServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(assignmentResultDto);

        var returnedTestRunInfo = new AssignmentTestRunInfoDto();
        _assignmentServiceMock
            .Setup(service => service.GetUserTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(returnedTestRunInfo);

        var returnedSolutionFiles = new List<SolutionFile>();
        _solutionFileRepositoryMock
            .Setup(repo => repo.GetAllLatestOfAssignmentForUserAsync(existingAssignment.Id, userId, null))
            .ReturnsAsync(returnedSolutionFiles);

        var returnedModel = new AssignmentDetailModel();
        _assignmentConverterMock
            .Setup(converter =>
                converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(),
                    It.IsAny<IList<TestResult>>(), It.IsAny<IList<SolutionFile>>()))
            .Returns(returnedModel);

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForUser(existingAssignment.Id, userId, null).Result as OkObjectResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id), Times.Once);
        _assignmentServiceMock.Verify(repo => repo.GetResultsForUserAsync(existingAssignment.Id, userId, null), Times.Once);
        _solutionFileRepositoryMock.Verify();
        _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(
            existingAssignment, returnedTestRunInfo, assignmentResultDto.TestResults, returnedSolutionFiles), Times.Once);
    }

    [Test]
    public void GetAssignmentResultsForUser_ShouldForbidStudentsToSeeResultsOfOthers()
    {
        //Arrange
        var assignmentId = _random.NextPositive();
        var userId = _random.NextPositive();
        var otherUserId = userId + 1;

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForUser(assignmentId, otherUserId, null).Result as ForbidResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
        _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(
            It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
            It.IsAny<IList<SolutionFile>>()), Times.Never);
    }

    [Test]
    public void GetAssignmentResultsForUser_ShouldForbidUsersWithoutRole()
    {
        //Arrange
        var assignmentId = _random.NextPositive();
        var userId = _random.NextPositive();

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForUser(assignmentId, userId, null).Result as ForbidResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
        _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), null), Times.Never);
        _assignmentConverterMock.Verify(
            converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                It.IsAny<IList<SolutionFile>>()), Times.Never);
    }

    [Test]
    public void GetAssignmentResultsForTeam_ShouldReturnAssignmentDetailsIfParametersAreValid()
    {
        //Arrange
        var userId = _random.NextPositive();
        var teamId = _random.NextPositive();
        var project = new ProjectBuilder().WithId().Build();
        var existingTeam = new ProjectTeamBuilder().WithId().WithProject(project).WithUser(userId).Build();
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(existingTeam);

        var existingAssignment = new AssignmentBuilder().WithId().WithTopic(project).Build();
        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()))
            .ReturnsAsync(existingAssignment);

        var assignmentResultDto = new AssignmentResultDto();
        _assignmentServiceMock.Setup(service => service.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(assignmentResultDto);

        var returnedTestRunInfo = new AssignmentTestRunInfoDto();
        _assignmentServiceMock
            .Setup(service => service.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(returnedTestRunInfo);

        var returnedSolutionFiles = new List<SolutionFile>();
        _solutionFileRepositoryMock
            .Setup(repo => repo.GetAllLatestOfAssignmentForTeamAsync(existingAssignment.Id, teamId, null))
            .ReturnsAsync(returnedSolutionFiles);

        var returnedModel = new AssignmentDetailModel();
        _assignmentConverterMock
            .Setup(converter =>
                converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(),
                    It.IsAny<IList<TestResult>>(), It.IsAny<IList<SolutionFile>>()))
            .Returns(returnedModel);

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForTeam(existingAssignment.Id, teamId, null).Result as OkObjectResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
        _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(teamId), Times.Once);
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id), Times.Once);
        _solutionFileRepositoryMock.Verify();
        _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(existingAssignment.Id, teamId, null), Times.Once);
        _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(existingTeam.ProjectId, existingAssignment.Id, teamId, null), Times.Once);
        _assignmentConverterMock.Verify(
            converter => converter.ToAssignmentDetailModel(existingAssignment, returnedTestRunInfo,
                assignmentResultDto.TestResults, returnedSolutionFiles), Times.Once);
    }

    [Test]
    public void GetAssignmentResultsForTeam_ShouldForbidStudentsToSeeResultsOfOtherTeams()
    {
        //Arrange
        var assignmentId = _random.NextPositive();
        var userId = _random.NextPositive();
            
        var otherUserId = _random.NextPositive();
        var otherTeam = new ProjectTeamBuilder().WithId().WithUser(otherUserId).Build();
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(otherTeam);

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForTeam(assignmentId, otherTeam.Id, null).Result as ForbidResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(otherTeam.Id), Times.Once);
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
        _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        _assignmentConverterMock.Verify(
            converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                It.IsAny<IList<SolutionFile>>()), Times.Never);
    }

    [Test]
    public void GetAssignmentResultsForTeam_ShouldForbidUsersWithoutRole()
    {
        //Arrange
        var assignmentId = _random.NextPositive();
        var userId = _random.NextPositive();
        var teamId = new ProjectTeamBuilder().WithId().WithUser(userId).Build();
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(teamId);

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

        //Act
        var actionResult = _controller.GetAssignmentResultsForTeam(assignmentId, teamId.Id, null).Result as ForbidResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(teamId.Id), Times.Once);
        _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
        _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        _assignmentConverterMock.Verify(
            converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                It.IsAny<IList<SolutionFile>>()), Times.Never);
    }

    [Test]
    public async Task Delete_TestExists_ShouldUseRepoToDeleteTest_ShouldReturnOk()
    {
        // Arrange
        Assignment assignmentToDelete = new AssignmentBuilder().WithId().Build();
        _assignmentRepositoryMock.Setup(r => r.GetByIdAsync(assignmentToDelete.Id)).ReturnsAsync(assignmentToDelete);

        // Act
        var result = (await _controller.Delete(assignmentToDelete.Id)) as OkResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _assignmentRepositoryMock.Verify(r => r.DeleteAsync(assignmentToDelete), Times.Once);
    }

    [Test]
    public async Task Delete_TestDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        int assignmentId = Random.Shared.NextPositive();
        _assignmentRepositoryMock.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(() => null);

        // Act
        var result = (await _controller.Delete(assignmentId)) as NotFoundResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _assignmentRepositoryMock.Verify(r => r.GetByIdAsync(assignmentId), Times.Once);
    }

    [Test]
    public async Task GetAssignmentResults_ShouldDelegateToGetAssignmentResultsForUserWithCallerUserId()
    {
        // Arrange
        var existingAssignment = new AssignmentBuilder().WithId().Build();
        var userId = _random.NextPositive();
        var date = DateTime.UtcNow.AddDays(-1);

        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id))
            .ReturnsAsync(existingAssignment);

        var assignmentResultDto = new AssignmentResultDto();
        _assignmentServiceMock.Setup(service => service.GetResultsForUserAsync(existingAssignment.Id, userId, date.ToUniversalTime()))
            .ReturnsAsync(assignmentResultDto);

        var returnedTestRunInfo = new AssignmentTestRunInfoDto();
        _assignmentServiceMock
            .Setup(service => service.GetUserTestRunInfoForAssignment(existingAssignment.Id, userId, date.ToUniversalTime()))
            .ReturnsAsync(returnedTestRunInfo);

        var returnedSolutionFiles = new List<SolutionFile>();
        _solutionFileRepositoryMock
            .Setup(repo => repo.GetAllLatestOfAssignmentForUserAsync(existingAssignment.Id, userId, date.ToUniversalTime()))
            .ReturnsAsync(returnedSolutionFiles);

        var returnedModel = new AssignmentDetailModel();
        _assignmentConverterMock
            .Setup(converter => converter.ToAssignmentDetailModel(
                existingAssignment, returnedTestRunInfo, assignmentResultDto.TestResults, returnedSolutionFiles))
            .Returns(returnedModel);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(userId.ToString())
            .WithRole(Role.Constants.Student)
            .Build();

        // Act
        var actionResult = await _controller.GetAssignmentResults(existingAssignment.Id, date) as OkObjectResult;

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.SameAs(returnedModel));
        _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(existingAssignment.Id, userId, date.ToUniversalTime()), Times.Once);
    }

    [Test]
    public async Task GetAssignmentResultsForUser_WhenCallerIsLector_ShouldAllowViewingOtherUserResults()
    {
        // Arrange
        var existingAssignment = new AssignmentBuilder().WithId().Build();
        var lectorId = _random.NextPositive();
        var studentId = lectorId + 1;

        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id))
            .ReturnsAsync(existingAssignment);

        var assignmentResultDto = new AssignmentResultDto();
        _assignmentServiceMock.Setup(service => service.GetResultsForUserAsync(existingAssignment.Id, studentId, null))
            .ReturnsAsync(assignmentResultDto);

        var returnedTestRunInfo = new AssignmentTestRunInfoDto();
        _assignmentServiceMock
            .Setup(service => service.GetUserTestRunInfoForAssignment(existingAssignment.Id, studentId, null))
            .ReturnsAsync(returnedTestRunInfo);

        var returnedSolutionFiles = new List<SolutionFile>();
        _solutionFileRepositoryMock
            .Setup(repo => repo.GetAllLatestOfAssignmentForUserAsync(existingAssignment.Id, studentId, null))
            .ReturnsAsync(returnedSolutionFiles);

        var returnedModel = new AssignmentDetailModel();
        _assignmentConverterMock
            .Setup(converter => converter.ToAssignmentDetailModel(
                existingAssignment, returnedTestRunInfo, assignmentResultDto.TestResults, returnedSolutionFiles))
            .Returns(returnedModel);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(lectorId.ToString())
            .WithRole(Role.Constants.Lector)
            .Build();

        // Act
        var actionResult = await _controller.GetAssignmentResultsForUser(existingAssignment.Id, studentId, null) as OkObjectResult;

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.SameAs(returnedModel));
    }

    [Test]
    public async Task GetAssignmentResultsForTeam_WhenCallerIsLector_ShouldReturnTeamAssignmentDetails()
    {
        // Arrange
        var lectorId = _random.NextPositive();
        var studentId = lectorId + 1;
        var project = new ProjectBuilder().WithId().Build();
        var existingTeam = new ProjectTeamBuilder().WithId().WithProject(project).WithUser(studentId).Build();
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(existingTeam.Id)).ReturnsAsync(existingTeam);

        var existingAssignment = new AssignmentBuilder().WithId().WithTopic(project).Build();
        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id))
            .ReturnsAsync(existingAssignment);

        var assignmentResultDto = new AssignmentResultDto();
        _assignmentServiceMock.Setup(service => service.GetResultsForTeamAsync(existingTeam.ProjectId, existingAssignment.Id, existingTeam.Id, null))
            .ReturnsAsync(assignmentResultDto);

        var returnedTestRunInfo = new AssignmentTestRunInfoDto();
        _assignmentServiceMock
            .Setup(service => service.GetTeamTestRunInfoForAssignment(existingAssignment.Id, existingTeam.Id, null))
            .ReturnsAsync(returnedTestRunInfo);

        var returnedSolutionFiles = new List<SolutionFile>();
        _solutionFileRepositoryMock
            .Setup(repo => repo.GetAllLatestOfAssignmentForTeamAsync(existingAssignment.Id, existingTeam.Id, null))
            .ReturnsAsync(returnedSolutionFiles);

        var returnedModel = new AssignmentDetailModel();
        _assignmentConverterMock
            .Setup(converter => converter.ToAssignmentDetailModel(
                existingAssignment, returnedTestRunInfo, assignmentResultDto.TestResults, returnedSolutionFiles))
            .Returns(returnedModel);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(lectorId.ToString())
            .WithRole(Role.Constants.Lector)
            .Build();

        // Act
        var actionResult = await _controller.GetAssignmentResultsForTeam(existingAssignment.Id, existingTeam.Id, null) as OkObjectResult;

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.SameAs(returnedModel));
    }

    [Test]
    public async Task GetAssignmentsOfCourse_ShouldReturnMappedAssignmentsFromAllCourseTopics()
    {
        // Arrange
        int courseId = _random.NextPositive();
        var chapter = new ChapterBuilder().WithId().WithAssignments(2, 1).Build();
        var project = new ProjectBuilder().WithId().WithAssignments(1).Build();
        var topics = new List<Guts.Domain.TopicAggregate.ITopic> { chapter, project };

        _topicServiceMock.Setup(service => service.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId))
            .ReturnsAsync(topics);

        var mappedModel = new TopicAssignmentModel();
        _mapperMock.Setup(mapper => mapper.MapToTopicAssignmentModel(It.IsAny<Assignment>()))
            .Returns(mappedModel);

        // Act
        var actionResult = await _controller.GetAssignmentsOfCourse(courseId) as OkObjectResult;

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        var models = actionResult.Value as IEnumerable<TopicAssignmentModel>;
        Assert.That(models, Is.Not.Null);
        var modelList = new List<TopicAssignmentModel>(models);
        Assert.That(modelList, Has.Count.EqualTo(3));
        _topicServiceMock.Verify(service => service.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId), Times.Once);
        _mapperMock.Verify(mapper => mapper.MapToTopicAssignmentModel(It.IsAny<Assignment>()), Times.Exactly(3));
    }

    [Test]
    public async Task DownloadSourceCodesAsZip_ShouldReturnZipFileContentResult()
    {
        // Arrange
        int assignmentId = _random.NextPositive();
        var solutions = new List<SolutionDto> { new() };
        var zipBytes = new byte[] { 1, 2, 3, 4 };

        _assignmentServiceMock.Setup(service => service.GetAllSolutions(assignmentId))
            .ReturnsAsync(solutions);
        _solutionFileServiceMock.Setup(service => service.CreateZipFromFiles(solutions))
            .ReturnsAsync(zipBytes);

        // Act
        var result = await _controller.DownloadSourceCodesAsZip(assignmentId) as FileContentResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContentType, Is.EqualTo("application/zip"));
        Assert.That(result.FileContents, Is.EqualTo(zipBytes));
        Assert.That(result.FileDownloadName, Is.EqualTo($"Assignment_{assignmentId}_sources.zip"));
        _assignmentServiceMock.Verify(service => service.GetAllSolutions(assignmentId), Times.Once);
        _solutionFileServiceMock.Verify(service => service.CreateZipFromFiles(solutions), Times.Once);
    }

    [Test]
    public async Task GetAssignmentTimelineForUser_WhenAuthorized_ShouldReturnTimelineModel()
    {
        var userId = _random.NextPositive();
        var assignment = new AssignmentBuilder().WithId().Build();
        assignment.AddTest("T1", 1);
        assignment.AddTest("T2", 2);
        var queryDate = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();
        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignment.Id)).ReturnsAsync(assignment);
        _assignmentServiceMock
            .Setup(s => s.GetTimelineForUserAsync(assignment.Id, userId, 2, queryDate))
            .ReturnsAsync(new AssignmentTimelineDto(
                assignment.Id,
                2,
                [new TestRunTimelinePoint(10, 1, queryDate, 1, 1, 2, true, true, 0)]));

        var result = await _controller.GetAssignmentTimelineForUser(assignment.Id, userId, queryDate) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var model = result.Value as AssignmentTimelineModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model.AssignmentId, Is.EqualTo(assignment.Id));
        Assert.That(model.TotalTestsCount, Is.EqualTo(2));
        Assert.That(model.Runs, Has.Count.EqualTo(1));
        Assert.That(model.Runs[0].TestRunId, Is.EqualTo(10));
        Assert.That(model.Runs[0].SolutionFilesChanged, Is.True);
    }

    [Test]
    public async Task GetAssignmentTimelineForUser_WhenForbidden_ShouldReturnForbid()
    {
        _controller.ControllerContext = new ControllerContextBuilder().WithUser("1").WithRole(Role.Constants.Student).Build();
        var studentOtherResult = await _controller.GetAssignmentTimelineForUser(5, 2, null);
        Assert.That(studentOtherResult, Is.InstanceOf<ForbidResult>());

        _controller.ControllerContext = new ControllerContextBuilder().WithUser("1").Build(); // neither student nor lector
        var unknownRoleResult = await _controller.GetAssignmentTimelineForUser(5, 1, null);
        Assert.That(unknownRoleResult, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetAssignmentTimelineForTeam_WhenAuthorizedAndWhenForbidden_ShouldEnforceMembershipAndRoles()
    {
        var project = new ProjectBuilder().WithId().Build();
        var assignment = new AssignmentBuilder().WithId().WithTopic(project).Build();
        assignment.AddTest("T1", 1);
        var team = new ProjectTeamBuilder().WithId().WithProject(project).WithUser(10).Build();

        _projectTeamRepositoryMock.Setup(r => r.LoadByIdAsync(team.Id)).ReturnsAsync(team);
        _assignmentRepositoryMock.Setup(r => r.GetSingleWithTestsAndCourseAsync(assignment.Id)).ReturnsAsync(assignment);
        _assignmentServiceMock
            .Setup(s => s.GetTimelineForTeamAsync(assignment.Id, team.Id, 1, null))
            .ReturnsAsync(new AssignmentTimelineDto(assignment.Id, 1, []));

        // Lector allowed
        _controller.ControllerContext = new ControllerContextBuilder().WithUser("99").WithRole(Role.Constants.Lector).Build();
        var lectorResult = await _controller.GetAssignmentTimelineForTeam(assignment.Id, team.Id, null) as OkObjectResult;
        Assert.That(lectorResult, Is.Not.Null);

        // Student team member allowed
        _controller.ControllerContext = new ControllerContextBuilder().WithUser("10").WithRole(Role.Constants.Student).Build();
        var memberResult = await _controller.GetAssignmentTimelineForTeam(assignment.Id, team.Id, null) as OkObjectResult;
        Assert.That(memberResult, Is.Not.Null);

        // Student non-member forbidden
        _controller.ControllerContext = new ControllerContextBuilder().WithUser("20").WithRole(Role.Constants.Student).Build();
        var nonMemberResult = await _controller.GetAssignmentTimelineForTeam(assignment.Id, team.Id, null);
        Assert.That(nonMemberResult, Is.InstanceOf<ForbidResult>());

        // No role forbidden
        _controller.ControllerContext = new ControllerContextBuilder().WithUser("20").Build();
        var noRoleResult = await _controller.GetAssignmentTimelineForTeam(assignment.Id, team.Id, null);
        Assert.That(noRoleResult, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public void GetAssignmentResultsForTeam_WhenAssignmentDoesNotBelongToTeamsProject_ShouldThrowContractExceptionAndNotLeakData()
    {
        // Arrange
        var studentId = _random.NextPositive();
        var teammateId = studentId + 1;
        var project = new ProjectBuilder().WithId().Build();
        var chapter = new ChapterBuilder().WithId().Build();
        while (chapter.Id == project.Id)
        {
            chapter = new ChapterBuilder().WithId().Build();
        }

        var team = new ProjectTeamBuilder().WithId().WithProject(project).WithUser(studentId).WithUser(teammateId).Build();
        var individualChapterAssignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(team.Id)).ReturnsAsync(team);
        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(individualChapterAssignment.Id))
            .ReturnsAsync(individualChapterAssignment);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(studentId.ToString())
            .WithRole(Role.Constants.Student)
            .Build();

        // Act & Assert
        Assert.That(
            async () => await _controller.GetAssignmentResultsForTeam(individualChapterAssignment.Id, team.Id, null),
            Throws.InstanceOf<ContractException>());

        _assignmentServiceMock.Verify(
            service => service.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()),
            Times.Never);
        _assignmentServiceMock.Verify(
            service => service.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()),
            Times.Never);
        _solutionFileRepositoryMock.Verify(
            repo => repo.GetAllLatestOfAssignmentForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()),
            Times.Never);
    }

    [Test]
    public void GetAssignmentTimelineForTeam_WhenAssignmentDoesNotBelongToTeamsProject_ShouldThrowContractExceptionAndNotLeakData()
    {
        // Arrange
        var studentId = _random.NextPositive();
        var project = new ProjectBuilder().WithId().Build();
        var chapter = new ChapterBuilder().WithId().Build();
        while (chapter.Id == project.Id)
        {
            chapter = new ChapterBuilder().WithId().Build();
        }

        var team = new ProjectTeamBuilder().WithId().WithProject(project).WithUser(studentId).Build();
        var individualChapterAssignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(team.Id)).ReturnsAsync(team);
        _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(individualChapterAssignment.Id))
            .ReturnsAsync(individualChapterAssignment);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(studentId.ToString())
            .WithRole(Role.Constants.Student)
            .Build();

        // Act & Assert
        Assert.That(
            async () => await _controller.GetAssignmentTimelineForTeam(individualChapterAssignment.Id, team.Id, null),
            Throws.InstanceOf<ContractException>());

        _assignmentServiceMock.Verify(
            service => service.GetTimelineForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()),
            Times.Never);
    }
}