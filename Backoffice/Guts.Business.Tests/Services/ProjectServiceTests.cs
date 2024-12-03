using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ProjectServiceTests
    {
        private ProjectService _service;
        private Mock<IProjectRepository> _projectRepositoryMock;
        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
        private Mock<IAssignmentService> _assignmentServiceMock;
        private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;
        private Mock<IProjectAssessmentFactory> _projectAssessmentFactoryMock;
        private Mock<IProjectAssessmentRepository> _projectAssessmentRepositoryMock;
        private Mock<ITestRunRepository> _testRunRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _solutionFileRepositoryMock = new Mock<ISolutionFileRepository>();
            _projectAssessmentFactoryMock = new Mock<IProjectAssessmentFactory>();
            _projectAssessmentRepositoryMock = new Mock<IProjectAssessmentRepository>();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();

            _service = new ProjectService(_projectRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _periodRepositoryMock.Object,
                null,
                _projectTeamRepositoryMock.Object,
                _solutionFileRepositoryMock.Object,
                _assignmentServiceMock.Object,
                _projectAssessmentRepositoryMock.Object, 
                _projectAssessmentFactoryMock.Object,
                _testRunRepositoryMock.Object);
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldReturnProjectIfItExists()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            string courseCode = Guid.NewGuid().ToString();
            Project existingProject = new ProjectBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingProject.Id)).ReturnsAsync(existingPeriod);

            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            IProject result = _service.GetOrCreateProjectAsync(courseCode, existingProject.Code, existingProject.Id).Result;

            //Assert
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingProject.Code, existingPeriod.Id), Times.Once());
            _projectRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Project>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldThrowDataNotFoundExceptionWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            string courseCode = Guid.NewGuid().ToString();
            Project existingProject = new ProjectBuilder().WithId().WithCourse(courseCode).Build();
            int periodId = Random.Shared.NextPositive();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(It.IsAny<int?>())).Throws<DataNotFoundException>();

            //Act + Assert
            Assert.That(() => _service.GetOrCreateProjectAsync(courseCode, existingProject.Code, periodId).Result,
                Throws.InstanceOf<AggregateException>().With.Matches((AggregateException ex) =>
                    ex.InnerExceptions.OfType<DataNotFoundException>().Any()));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldCreateProjectIfItDoesNotExist()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Course existingCourse = new CourseBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _courseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>())).ReturnsAsync(existingCourse);

            _projectRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<Code>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            Project addedProject = new ProjectBuilder().WithId().Build();
            _projectRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Project>())).ReturnsAsync(addedProject);

            //Act
            IProject result = _service.GetOrCreateProjectAsync(existingCourse.Code, addedProject.Code, null).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(null), Times.Once);
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
            int courseId = Random.Shared.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(It.IsAny<int?>())).Throws<DataNotFoundException>();

            //Act
            IReadOnlyList<IProject> result = _service.GetProjectsOfCourseAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(null), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetProjectsOfCourseAsync_ShouldRetrieveChaptersFromRepository()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            int courseId = Random.Shared.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);

            var projectsOfCourse = new List<Project>();
            if (projectsOfCourse == null) throw new ArgumentNullException(nameof(projectsOfCourse));
            _projectRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(projectsOfCourse);

            //Act
            IReadOnlyList<IProject> result = _service.GetProjectsOfCourseAsync(courseId, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(courseId, existingPeriod.Id), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetProjectsOfCourseAsyncShouldSortProjectsByDescription()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            int courseId = Random.Shared.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);

            List<Project> projectsOfCourse = new List<Project>
            {
                new ProjectBuilder().WithDescription("zzz").Build(),
                new ProjectBuilder().WithDescription("aaa").Build()
            };
            _projectRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(projectsOfCourse);

            //Act
            IReadOnlyList<IProject> results = _service.GetProjectsOfCourseAsync(courseId, null).Result;

            //Assert
            Assert.That(results.Count, Is.EqualTo(projectsOfCourse.Count));
            Assert.That(results.ElementAt(0).Description, Is.LessThan(results.ElementAt(1).Description));
        }

        [Test]
        public void LoadProjectAsync_ShouldReturnProjectForCurrentPeriodWithAssignmentsAndTeams()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            IProject result = _service.LoadProjectAsync(existingProject.CourseId, existingProject.Code, null).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(null), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTeamsAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id), Times.Once());
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void LoadProjectForUserAsync_ShouldReturnProjectForCurrentPeriodWithAssignmentsAndOnlyTeamsOfTheUser()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsOfUserAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            IProject result = _service.LoadProjectForUserAsync(existingProject.CourseId, existingProject.Code, userId, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTeamsOfUserAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id, userId), Times.Once());
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GenerateTeamsForProject_ShouldCreateTeamsAndLinkThemToTheProject()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder()
                .WithId()
                .WithTeams(0)
                .Build();
            string teamBaseName = Random.Shared.NextString();
            int from = Random.Shared.Next(5, 101);
            int to = from + Random.Shared.Next(3, 10);
            List<string> expectedTeamNumbers = new List<string>();
            for (int i = from; i <= to; i++)
            {
                expectedTeamNumbers.Add(i.ToString());
            }

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            _service.GenerateTeamsForProject(existingProject.CourseId, existingProject.Code, teamBaseName, from, to).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<ProjectTeam>(projectTeam => projectTeam.Name.StartsWith(teamBaseName) 
                                                      && expectedTeamNumbers.Any(number => projectTeam.Name.Contains(number))
                                                      && projectTeam.Name.Length > teamBaseName.Length
                                                      && projectTeam.ProjectId == existingProject.Id)
                ),
                Times.Exactly(expectedTeamNumbers.Count));
        }

        [Test]
        public void GenerateTeamsForProject_ShouldNotCreateTeamsThatAlreadyExist()
        {
            //Arrange
            string teamBaseName = Random.Shared.NextString();
            int from = Random.Shared.Next(5, 101);
            int to = from + 1;

            ProjectTeam existingTeam = new ProjectTeamBuilder().WithId().WithName($"{teamBaseName} {to}").Build();

            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder()
                .WithId()
                .WithTeam(existingTeam)
                .Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo =>
                    repo.LoadWithAssignmentsAndTeamsAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            _service.GenerateTeamsForProject(existingProject.CourseId, existingProject.Code, teamBaseName, from, to).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(
                repo => repo.AddAsync( It.Is<ProjectTeam>(projectTeam => projectTeam.Name == $"{teamBaseName} {from}")),
                Times.Once);

            _projectTeamRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<ProjectTeam>(projectTeam => projectTeam.Name == $"{teamBaseName} {to}")),
                Times.Never);
        }

        [Test]
        public void LoadTeamsOfProject_ShouldLoadTheTeamsOfTheProjectForTheCurrentPeriodWithUsers()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();
            List<ProjectTeam> existingTeams = new List<ProjectTeam>();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);
            _projectTeamRepositoryMock.Setup(repo => repo.GetByProjectWithUsersAsync(It.IsAny<int>())).ReturnsAsync(existingTeams);

            //Act
            IReadOnlyList<IProjectTeam> result = _service.LoadTeamsOfProjectAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.CourseId, existingProject.Code, existingPeriod.Id), Times.Once);
            _projectTeamRepositoryMock.Verify(repo => repo.GetByProjectWithUsersAsync(existingProject.Id), Times.Once);
            Assert.That(result, Is.EqualTo(existingTeams));
        }

        [Test]
        public void AddUserToProjectTeamAsync_UserNotInTeamYet_ShouldUseTheProjectTeamRepository()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();
            ProjectTeam team = new ProjectTeamBuilder().WithId().Build();
            List<ProjectTeam> existingTeams = new List<ProjectTeam>(){team};

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);
            _projectTeamRepositoryMock.Setup(repo => repo.GetByProjectWithUsersAsync(It.IsAny<int>())).ReturnsAsync(existingTeams);

            int userId = Random.Shared.NextPositive();

            //Act
            _service.AddUserToProjectTeamAsync(existingProject.CourseId, existingProject.Code, team.Id, userId).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(repo => repo.AddUserToTeam(team.Id, userId), Times.Once);
        }

        [Test]
        public void AddUserToProjectTeamAsync_UserAlreadyInOtherTeam_ShouldThrowContractException()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();
            ProjectTeam team = new ProjectTeamBuilder().WithId().WithUser(userId).Build();
            List<ProjectTeam> existingTeams = new List<ProjectTeam>() { team };

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);
            _projectTeamRepositoryMock.Setup(repo => repo.GetByProjectWithUsersAsync(It.IsAny<int>())).ReturnsAsync(existingTeams);

            
            //Act + Assert
            Assert.That(() => _service.AddUserToProjectTeamAsync(existingProject.CourseId, existingProject.Code, team.Id, userId),
                               Throws.InstanceOf<ContractException>());
            
            _projectTeamRepositoryMock.Verify(repo => repo.AddUserToTeam(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void AddUserToProjectTeamAsync_TeamIsNotPartOfTheProject_ShouldThrowContractException()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build();
            ProjectTeam otherProjectTeam = new ProjectTeamBuilder().WithId().Build();
            List<ProjectTeam> existingTeams = new List<ProjectTeam>();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);
            _projectTeamRepositoryMock.Setup(repo => repo.GetByProjectWithUsersAsync(It.IsAny<int>())).ReturnsAsync(new List<ProjectTeam>());


            //Act + Assert
            Assert.That(() => _service.AddUserToProjectTeamAsync(existingProject.CourseId, existingProject.Code, otherProjectTeam.Id, userId),
                Throws.InstanceOf<ContractException>());

            _projectTeamRepositoryMock.Verify(repo => repo.AddUserToTeam(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void GetResultsForTeamAsync_ShouldAccumulateTheResultsOnTheAssignmentsOfTheProject()
        {
            //Arrange
            int numberOfAssignments = Random.Shared.Next(2, 11);
            Project existingProject = new ProjectBuilder()
                .WithId()
                .WithAssignments(numberOfAssignments)
                .Build();
            int teamId = Random.Shared.NextPositive();
            DateTime date = DateTime.UtcNow;

            _assignmentServiceMock.Setup(
                    service => service.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>())
                )
                .ReturnsAsync(new AssignmentResultDto());

            //Act
            IReadOnlyList<AssignmentResultDto> assignmentResults = _service.GetResultsForTeamAsync(existingProject, teamId, date).Result;

            //Assert
            Assert.That(assignmentResults.Count, Is.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(service => service.GetResultsForTeamAsync(
                existingProject.Id,
                It.IsIn<int>(existingProject.Assignments.Select(a => a.Id)),
                teamId, date), Times.Exactly(numberOfAssignments));
        }

        [Test]
        public void GetProjectStatisticsAsync_ShouldAccumulateStatisticsOnTheAssignmentsOfTheProject()
        {
            //Arrange
            int numberOfAssignments = Random.Shared.Next(2, 11);
            Project existingProject = new ProjectBuilder()
                .WithId()
                .WithAssignments(numberOfAssignments)
                .Build();
            DateTime date = DateTime.UtcNow;

            _assignmentServiceMock.Setup(
                    service => service.GetAssignmentTeamStatisticsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>())
                )
                .ReturnsAsync(new AssignmentStatisticsDto());

            //Act
            IReadOnlyList<AssignmentStatisticsDto> assignmentStatistics = _service.GetProjectStatisticsAsync(existingProject, date).Result;

            //Assert
            Assert.That(assignmentStatistics.Count, Is.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(service => service.GetAssignmentTeamStatisticsAsync(
                existingProject.Id,
                It.IsIn<int>(existingProject.Assignments.Select(a => a.Id)),
                date), Times.Exactly(numberOfAssignments));
        }

        [Test]
        public void CreateProjectAssessmentAsync_ShouldUseFactoryToCreateAndRepositoryToSave()
        {
            //Arrange
            int projectId = Random.Shared.NextPositive();
            string description = Random.Shared.NextString();
            DateTime openOnUtc = Random.Shared.NextDateTimeInFuture().ToUniversalTime();
            DateTime deadlineUtc = openOnUtc.AddDays(7);
          
            IProjectAssessment createdAssessment = new ProjectAssessmentMockBuilder().Build().Object;
            _projectAssessmentFactoryMock
                .Setup(factory => factory.CreateNew(projectId, description, openOnUtc, deadlineUtc))
                .Returns(createdAssessment);

            //Act
            IProjectAssessment result = _service.CreateProjectAssessmentAsync(projectId, description, openOnUtc, deadlineUtc).Result;

            //Assert
            _projectAssessmentRepositoryMock.Verify(repo => repo.AddAsync(createdAssessment), Times.Once);
            Assert.That(result, Is.SameAs(createdAssessment));
        }

        [Test]
        public void RemoveUserFromProjectTeamAsync_UseRepositoryAndRemoveProjectTestRunsOfUser()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().WithTeams(1).Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTeamsOfUserAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            int teamId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();

            //Act
            _service.RemoveUserFromProjectTeamAsync(existingProject.CourseId, existingProject.Code, teamId, userId).Wait();

            //Assert
            _projectTeamRepositoryMock.Verify(repo => repo.RemoveUserFromTeam(teamId, userId), Times.Once);
            _testRunRepositoryMock.Verify(repo => repo.RemoveAllTopicTestRunsOfUserAsync(existingProject.Id, userId),
                Times.Once);
        }

        [Test]
        public void RemoveUserFromProjectTeamAsync_UserNotPartOfTheTeam_ShouldThrowContractException()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Project existingProject = new ProjectBuilder().WithId().Build(); //no teams, so user is not part of the team

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTeamsOfUserAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            int teamId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();

            //Act + Assert
            Assert.That(
                () => _service.RemoveUserFromProjectTeamAsync(existingProject.CourseId, existingProject.Code, teamId,
                    userId), Throws.InstanceOf<ContractException>());
        }
    }
}