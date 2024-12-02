using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Tests.Builders;
using Guts.Business.Repositories;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

public class UserControllerTests
{
    private UserController _controller;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock = null!;
    private int _userId;
    private string _email = null!;
    private string _role = null!;
    private List<ProjectTeam> _teams = null!;

    [SetUp]
    public void Setup()
    {
        _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
        _controller = new UserController(_projectTeamRepositoryMock.Object);
        _userId = Random.Shared.NextPositive();
        _role = Role.Constants.Student;
        _email = Guid.NewGuid().ToString();

        _teams = new List<ProjectTeam>()
        {
            new ProjectTeamBuilder().WithId().WithUser(_userId).Build(),
            new ProjectTeamBuilder().WithId().WithUser(_userId).Build()
        };
        _projectTeamRepositoryMock.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync(_teams);

        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(_userId.ToString())
            .WithRole(_role)
            .WithEmail(_email)
            .Build();
    }

    [Test]
    public async Task GetCurrentUserProfile_ShouldRetrieveTeamsOfUser_ShouldReturnProfileInfo()
    {
        // Act
        var result =  (await _controller.GetCurrentUserProfile()) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var model = result.Value as UserProfileModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(_userId));
        Assert.That(model.Email, Is.EqualTo(_email));
        Assert.That(model.Roles, Does.Contain(_role));
        Assert.That(model.Teams, Has.All.Matches<int>(teamId => _teams.Any(t => t.Id == teamId)));
    }
}