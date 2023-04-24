using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Models.ProjectModels;
using Guts.Api.Tests.Builders;
using Guts.Business.Dtos;
using Guts.Business.Services.Assessment;
using Guts.Common.Extensions;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.Tests.Builders;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ProjectTeamAssessmentControllerTests
{
    private Random _random;
    private int _userId;
    private ProjectTeamAssessmentController _controller;
    private Mock<IProjectTeamAssessmentService> _projectTeamAssessmentServiceMock;
    private Mock<IMapper> _mapperMock;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _userId = _random.NextPositive();
        _projectTeamAssessmentServiceMock = new Mock<IProjectTeamAssessmentService>();
        _mapperMock = new Mock<IMapper>();
        _controller = CreateControllerWithUserInContext(Role.Constants.Student);
    }

    [Test]
    public void GetProjectTeamAssessmentStatus_ShouldReturnResultRetrievedFromService()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();

        var statusDto = new ProjectTeamAssessmentStatusDto();

        _projectTeamAssessmentServiceMock.Setup(service => service.GetStatusAsync(projectAssessmentId, teamId))
            .ReturnsAsync(statusDto);

        //Act
        OkObjectResult result = _controller.GetProjectTeamAssessmentStatus(projectAssessmentId, teamId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(statusDto));
    }

    [Test]
    public void GetPeerAssessmentsOfUser_ShouldMapResultsRetrievedFromService()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();

        var existingAssessments = new List<IPeerAssessment>
        {
            new PeerAssessmentBuilder().Build(),
            new PeerAssessmentBuilder().Build(),
            new PeerAssessmentBuilder().Build()
        };

        _projectTeamAssessmentServiceMock.Setup(service => service.GetPeerAssessmentsOfUserAsync(projectAssessmentId, teamId, _userId))
            .ReturnsAsync(existingAssessments);

        var model = new PeerAssessmentModel();
        _mapperMock.Setup(mapper => mapper.Map<PeerAssessmentModel>(It.IsAny<object>())).Returns(model);

        //Act
        OkObjectResult result = _controller.GetPeerAssessmentsOfUser(projectAssessmentId, teamId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModels = result.Value as IList<PeerAssessmentModel>;
        Assert.That(returnedModels, Is.Not.Null);
        Assert.That(returnedModels, Has.Count.EqualTo(existingAssessments.Count));
        foreach (IPeerAssessment existingAssessment in existingAssessments)
        {
            _mapperMock.Verify(mapper => mapper.Map<PeerAssessmentModel>(existingAssessment), Times.Once);
        }
    }

    [Test]
    public void SavePeerAssessment_ShouldMapInputToDtosAndUseServiceToSave()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();

        var inputModels = new PeerAssessmentModel[]
        {
            new PeerAssessmentModel(),
            new PeerAssessmentModel(),
            new PeerAssessmentModel()
        };

        var mappedDto = new PeerAssessmentDto();

        _mapperMock.Setup(mapper => mapper.Map<PeerAssessmentDto>(It.IsAny<object>())).Returns(mappedDto);

        //Act
        OkResult result = _controller.SavePeerAssessment(projectAssessmentId, teamId, inputModels).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _mapperMock.Verify(mapper => mapper.Map<PeerAssessmentDto>(It.IsIn(inputModels)), Times.Exactly(inputModels.Length));

        _projectTeamAssessmentServiceMock.Verify(service => service.SavePeerAssessmentsOfUserAsync(projectAssessmentId,
            teamId, _userId, It.Is<IReadOnlyList<PeerAssessmentDto>>(dtos => dtos.Any(dto => dto == mappedDto))));
    }

    [Test]
    public void GetProjectTeamAssessmentResults_ShouldMapResultsRetrievedFromService()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();

        var assessmentResults = new List<IAssessmentResult>
        {
            new Mock<IAssessmentResult>().Object,
            new Mock<IAssessmentResult>().Object,
            new Mock<IAssessmentResult>().Object
        };

        _projectTeamAssessmentServiceMock.Setup(service => service.GetResultsForLectorAsync(projectAssessmentId, teamId))
            .ReturnsAsync(assessmentResults);

        var model = new AssessmentResultModel();
        _mapperMock.Setup(mapper => mapper.Map<AssessmentResultModel>(It.IsAny<object>())).Returns(model);

        //Act
        OkObjectResult result = _controller.GetProjectTeamAssessmentResults(projectAssessmentId, teamId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModels = result.Value as IList<AssessmentResultModel>;
        Assert.That(returnedModels, Is.Not.Null);
        Assert.That(returnedModels, Has.Count.EqualTo(assessmentResults.Count));
        foreach (IAssessmentResult assessmentResult in assessmentResults)
        {
            _mapperMock.Verify(mapper => mapper.Map<AssessmentResultModel>(assessmentResult), Times.Once);
        }
    }

    [Test]
    public void GetProjectTeamAssessmentResultForUser_ShouldMapResultsRetrievedFromService()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();

        IAssessmentResult assessmentResult = new Mock<IAssessmentResult>().Object;

        _projectTeamAssessmentServiceMock.Setup(service => service.GetResultForStudent(projectAssessmentId, teamId, _userId))
            .ReturnsAsync(assessmentResult);

        var model = new AssessmentResultModel();
        _mapperMock.Setup(mapper => mapper.Map<AssessmentResultModel>(It.IsAny<object>())).Returns(model);

        //Act
        OkObjectResult result = _controller.GetProjectTeamAssessmentResultForUser(projectAssessmentId, teamId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModel = result.Value as AssessmentResultModel;
        Assert.That(returnedModel, Is.Not.Null);
        _mapperMock.Verify(mapper => mapper.Map<AssessmentResultModel>(assessmentResult), Times.Once);
    }

    private ProjectTeamAssessmentController CreateControllerWithUserInContext(string role)
    {
        return new ProjectTeamAssessmentController(_projectTeamAssessmentServiceMock.Object, _mapperMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
        };
    }
}