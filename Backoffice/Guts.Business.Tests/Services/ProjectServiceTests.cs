using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ProjectServiceTests
    {
        private ProjectService _service;
        private Random _random;
        private Mock<IProjectRepository> _projectRepositoryMock;
        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
        private Mock<IAssignmentService> _assignmentServiceMock;
        private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _solutionFileRepositoryMock = new Mock<ISolutionFileRepository>();

            _service = new ProjectService(_projectRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _periodRepositoryMock.Object,
                _projectTeamRepositoryMock.Object,
                _solutionFileRepositoryMock.Object,
                _assignmentServiceMock.Object,
                null, null); //TODO: use actual mocks
        }

        //[Test]
        //public void GetProjectAsync_ShouldReturnProjectForCurrentPeriod()
        //{
        //    //Arrange
        //    var existingPeriod = new Period { Id = _random.NextPositive() };
        //    var courseCode = Guid.NewGuid().ToString();
        //    var existingProject = new ProjectBuilder().WithId()
        //        .WithCourse(courseCode)
        //        .WithPeriod(existingPeriod).Build();

        //    _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
        //    _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
        //        .ReturnsAsync(existingProject);

        //    //Act
        //    var result = _service.GetProjectAsync(courseCode, existingProject.Code).Result;

        //    //Assert
        //    _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
        //    _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingProject.Code, existingPeriod.Id), Times.Once());
        //    Assert.That(result, Is.EqualTo(existingProject));
        //}

        [Test]
        public void GetOrCreateProjectAsync_ShouldReturnProjectIfItExists()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseCode = Guid.NewGuid().ToString();
            var existingProject = new ProjectBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            var result = _service.GetOrCreateProjectAsync(courseCode, existingProject.Code).Result;

            //Assert
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingProject.Code, existingPeriod.Id), Times.Once());
            _projectRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Project>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldThrowDataNotFoundExceptionWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            var courseCode = Guid.NewGuid().ToString();
            var existingProject = new ProjectBuilder().WithId().WithCourse(courseCode).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).Throws<DataNotFoundException>();

            //Act + Assert
            Assert.That(() => _service.GetOrCreateProjectAsync(courseCode, existingProject.Code).Result,
                Throws.InstanceOf<AggregateException>().With.Matches((AggregateException ex) =>
                    ex.InnerExceptions.OfType<DataNotFoundException>().Any()));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldCreateProjectIfItDoesNotExist()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingCourse = new CourseBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _courseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>())).ReturnsAsync(existingCourse);

            _projectRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var addedProject = new ProjectBuilder().WithId().Build();
            _projectRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Project>())).ReturnsAsync(addedProject);

            //Act
            var result = _service.GetOrCreateProjectAsync(existingCourse.Code, addedProject.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code, addedProject.Code, existingPeriod.Id), Times.Once);
            _projectRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<Project>(p =>
                    p.PeriodId == existingPeriod.Id && p.CourseId == existingCourse.Id &&
                    p.Code == addedProject.Code)), Times.Once);

            _courseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code), Times.Once);

            Assert.That(result, Is.EqualTo(addedProject));
        }

        [Test]
        public void GetProjectsOfCourseAsync_ShouldReturnEmptyListWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).Throws<DataNotFoundException>();

            //Act
            var result = _service.GetProjectsOfCourseAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetProjectsOfCourseAsync_ShouldRetrieveChaptersFromRepository()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var projectsOfCourse = new List<Project>();
            _projectRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(projectsOfCourse);

            //Act
            var result = _service.GetProjectsOfCourseAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(courseId, existingPeriod.Id), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetProjectsOfCourseAsyncShouldSortProjectsByDescription()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var projectsOfCourse = new List<Project>
            {
                new ProjectBuilder().WithDescription("zzz").Build(),
                new ProjectBuilder().WithDescription("aaa").Build()
            };
            _projectRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(projectsOfCourse);

            //Act
            var results = _service.GetProjectsOfCourseAsync(courseId).Result;

            //Assert
            Assert.That(results.Count, Is.EqualTo(projectsOfCourse.Count));
            Assert.That(results.ElementAt(0).Description, Is.LessThan(results.ElementAt(1).Description));
        }

        [Test]
        public void LoadProjectAsync_ShouldReturnProjectForCurrentPeriodWithAssignmentsAndTeams()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            var result = _service.LoadProjectAsync(existingProject.CourseId, existingProject.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTeamsAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id), Times.Once());
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void LoadProjectForUserAsync_ShouldReturnProjectForCurrentPeriodWithAssignmentsAndOnlyTeamsOfTheUser()
        {
            //Arrange
            var userId = _random.NextPositive();
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsOfUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            var result = _service.LoadProjectForUserAsync(existingProject.CourseId, existingProject.Code, userId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTeamsOfUserAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id, userId), Times.Once());
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GenerateTeamsForProject_ShouldCreateTeamsAndLinkThemToTheProject()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder()
                .WithId()
                .WithTeams(0)
                .Build();
            var teamBaseName = _random.NextString();
            var numberOfTeams = _random.Next(5, 101);

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            _service.GenerateTeamsForProject(existingProject.CourseId, existingProject.Code, teamBaseName, numberOfTeams).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<ProjectTeam>(projectTeam => projectTeam.Name.StartsWith(teamBaseName)
                                                      && projectTeam.Name.Length > teamBaseName.Length
                                                      && projectTeam.ProjectId == existingProject.Id)
                ),
                Times.Exactly(numberOfTeams));
        }

        [Test]
        public void GenerateTeamsForProject_ShouldNotResultInTheProjectHavingMoreTeamsThanSpecified()
        {
            //Arrange
            var teamBaseName = _random.NextString();
            var desiredNumberOfTeams = _random.Next(20, 101);
            var actualNumberOfTeams = desiredNumberOfTeams - _random.Next(1, 20);
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder()
                .WithId()
                .WithTeams(actualNumberOfTeams)
                .Build();


            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            _service.GenerateTeamsForProject(existingProject.CourseId, existingProject.Code, teamBaseName, desiredNumberOfTeams).Wait();

            //Assert
            for (int teamNumber = actualNumberOfTeams + 1; teamNumber <= desiredNumberOfTeams; teamNumber++)
            {
                var number = teamNumber;
                _projectTeamRepositoryMock.Verify(
                    repo => repo.AddAsync(
                        It.Is<ProjectTeam>(projectTeam => projectTeam.Name == $"{teamBaseName} {number}"
                                                          && projectTeam.ProjectId == existingProject.Id)
                    ),
                    Times.Once);
            }
        }

        [Test]
        public void GenerateTeamsForProject_ShouldDoNothingWhenTheProjectAlreadyContainsTheDesiredAmountOfTeams()
        {
            //Arrange
            var teamBaseName = _random.NextString();
            var desiredNumberOfTeams = _random.Next(20, 101);
            var actualNumberOfTeams = desiredNumberOfTeams + 1;
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder()
                .WithId()
                .WithTeams(actualNumberOfTeams)
                .Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            _service.GenerateTeamsForProject(existingProject.CourseId, existingProject.Code, teamBaseName, desiredNumberOfTeams).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<ProjectTeam>()),
                Times.Never);
        }

        [Test]
        public void LoadTeamsOfProject_ShouldLoadTheTeamsOfTheProjectForTheCurrentPeriodWithUsers()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingProject = new ProjectBuilder().WithId().Build();
            var existingTeams = new List<ProjectTeam>();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);
            _projectTeamRepositoryMock.Setup(repo => repo.GetByProjectWithUsersAsync(It.IsAny<int>())).ReturnsAsync(existingTeams);

            //Act
            var result = _service.LoadTeamsOfProjectAsync(existingProject.CourseId, existingProject.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id), Times.Once);
            _projectTeamRepositoryMock.Verify(repo => repo.GetByProjectWithUsersAsync(existingProject.Id), Times.Once);
            Assert.That(result, Is.EqualTo(existingTeams));
        }

        [Test]
        public void AddUserToProjectTeamAsync_ShouldUseTheProjectTeamRepository()
        {
            //Arrange
            var teamId = _random.NextPositive();
            var userId = _random.NextPositive();

            //Act
            _service.AddUserToProjectTeamAsync(teamId, userId).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(repo => repo.AddUserToTeam(teamId, userId), Times.Once);
        }

        [Test]
        public void GetResultsForTeamAsync_ShouldAccumulateTheResultsOnTheAssignmentsOfTheProject()
        {
            //Arrange
            var numberOfAssignments = _random.Next(2, 11);
            var existingProject = new ProjectBuilder()
                .WithId()
                .WithAssignments(numberOfAssignments)
                .Build();
            var teamId = _random.NextPositive();
            var date = DateTime.UtcNow;

            _assignmentServiceMock.Setup(
                    service => service.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>())
                )
                .ReturnsAsync(new AssignmentResultDto());

            //Act
            var assignmentResults = _service.GetResultsForTeamAsync(existingProject, teamId, date).Result;

            //Assert
            Assert.That(assignmentResults.Count, Is.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(service => service.GetResultsForTeamAsync(
                It.IsIn<int>(existingProject.Assignments.Select(a => a.Id)),
                teamId, date), Times.Exactly(numberOfAssignments));
        }

        [Test]
        public void GetProjectStatisticsAsync_ShouldAccumulateStatisticsOnTheAssignmentsOfTheProject()
        {
            //Arrange
            var numberOfAssignments = _random.Next(2, 11);
            var existingProject = new ProjectBuilder()
                .WithId()
                .WithAssignments(numberOfAssignments)
                .Build();
            var date = DateTime.UtcNow;

            _assignmentServiceMock.Setup(
                    service => service.GetAssignmentTeamStatisticsAsync(It.IsAny<int>(), It.IsAny<DateTime?>())
                )
                .ReturnsAsync(new AssignmentStatisticsDto());

            //Act
            var assignmentStatistics = _service.GetProjectStatisticsAsync(existingProject, date).Result;

            //Assert
            Assert.That(assignmentStatistics.Count, Is.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(service => service.GetAssignmentTeamStatisticsAsync(
                It.IsIn<int>(existingProject.Assignments.Select(a => a.Id)),
                date), Times.Exactly(numberOfAssignments));
        }
    }
}