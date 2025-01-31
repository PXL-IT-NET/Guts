﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    internal class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IProjectTeamRepository _projectTeamRepository;
        private readonly ISolutionFileRepository _solutionFileRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly IProjectAssessmentRepository _projectAssessmentRepository;
        private readonly IProjectAssessmentFactory _assessmentFactory;
        private readonly ITestRunRepository _testRunRepository;

        public ProjectService(IProjectRepository projectRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository,
            ITopicRepository topicRepository,
            IProjectTeamRepository projectTeamRepository,
            ISolutionFileRepository solutionFileRepository,
            IAssignmentService assignmentService,
            IProjectAssessmentRepository projectAssessmentRepository,
            IProjectAssessmentFactory assessmentFactory,
            ITestRunRepository testRunRepository)
        {
            _projectRepository = projectRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _topicRepository = topicRepository;
            _projectTeamRepository = projectTeamRepository;
            _solutionFileRepository = solutionFileRepository;
            _assignmentService = assignmentService;
            _projectAssessmentRepository = projectAssessmentRepository;
            _assessmentFactory = assessmentFactory;
            _testRunRepository = testRunRepository;
        }

        public async Task<IProject> GetOrCreateProjectAsync(string courseCode, string projectCode, int? periodId = null)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(periodId);

            IProject project;
            try
            {
                project = await _projectRepository.GetSingleAsync(courseCode, projectCode, period.Id);
            }
            catch (DataNotFoundException)
            {
                Course course = await _courseRepository.GetSingleAsync(courseCode);
                project = new Project
                {
                    Code = projectCode,
                    CourseId = course.Id,
                    PeriodId = period.Id,
                    Description = projectCode
                };
                project = await _projectRepository.AddAsync(project);
            }

            return project;
        }

        public async Task<IProject> CreateProjectAsync(int courseId, Code projectCode,
            string description)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);

            IProject project;
            try
            {
                project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
                throw new ContractException($"A project with code {projectCode} already exists");
            }
            catch (DataNotFoundException)
            {
                project = new Project
                {
                    Code = projectCode,
                    CourseId = courseId,
                    PeriodId = period.Id,
                    Description = description
                };
                project = await _projectRepository.AddAsync(project);
            }
            return project;
        }

        public async Task UpdateProjectAsync(int courseId, string projectCode, string description)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);

            await _topicRepository.UpdateAsync(courseId, projectCode, period.Id, description);
        }

        public async Task<IReadOnlyList<IProject>> GetProjectsOfCourseAsync(int courseId, int? periodId = null)
        {
            try
            {
                IPeriod period = await _periodRepository.GetPeriodAsync(periodId);
                IReadOnlyList<IProject> projects = await _projectRepository.GetByCourseIdAsync(courseId, period.Id);
                return projects.OrderBy(p => p.Description).ToList();
            }
            catch (DataNotFoundException)
            {
                return new List<Project>();
            }
        }

        public async Task<IProject> LoadProjectAsync(int courseId, string projectCode, int? periodId = null)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(periodId);
            IProject project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);
            return project;
        }

        public async Task<IProject> LoadProjectForUserAsync(int courseId, string projectCode, int userId, int? periodId = null)
        {
            Contracts.Require(courseId > 0, "Invalid course identifier");
            Contracts.Require(!string.IsNullOrEmpty(projectCode), "Project code cannot be empty");
            Contracts.Require(userId > 0, "Invalid user id");

            IPeriod period = await _periodRepository.GetPeriodAsync(periodId);
            IProject project = await _projectRepository.LoadWithAssignmentsAndTeamsOfUserAsync(courseId, projectCode, period.Id, userId);
            return project;
        }

        public async Task<IProjectTeam> AddProjectTeamAsync(int courseId, string projectCode, string teamName)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);
            IProject project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);

            ProjectTeam newTeam = new ProjectTeam
            {
                Name = teamName,
                ProjectId = project.Id
            };

            ICollection<IProjectTeam> allTeams = project.Teams;
            bool alreadyExists = allTeams.Any(team => team.Name.ToLower() == newTeam.Name.ToLower());
            Contracts.Require(!alreadyExists, "A team with the same name already exists");

            return await _projectTeamRepository.AddAsync(newTeam);
        }

        public async Task UpdateProjectTeamAsync(int courseId, string projectCode, int teamId, string teamName)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);
            IProject project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
            IProjectTeam teamToUpdate = await _projectTeamRepository.LoadByIdAsync(teamId);

            Contracts.Require(teamToUpdate.ProjectId == project.Id, "The team does not belong to the project");

            teamToUpdate.Name = teamName;

            await _projectTeamRepository.UpdateAsync(teamToUpdate);
        }

        public async Task DeleteProjectTeamAsync(int courseId, string projectCode, int teamId)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);
            IProject project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
            IProjectTeam teamToDelete = await _projectTeamRepository.LoadByIdAsync(teamId);

            Contracts.Require(teamToDelete.ProjectId == project.Id, "The team does not belong to the project");

            await _projectTeamRepository.DeleteAsync(teamToDelete);
        }

        public async Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int teamNumberFrom, int teamNumberTo)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(null);
            IProject project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);

            ICollection<IProjectTeam> allTeams = project.Teams;

            for (int teamNumber = teamNumberFrom; teamNumber <= teamNumberTo; teamNumber++)
            {
                ProjectTeam newTeam = new ProjectTeam
                {
                    Name = $"{teamBaseName} {teamNumber:D2}",
                    ProjectId = project.Id
                };

                if (allTeams.All(team => team.Name.ToLower() != newTeam.Name.ToLower()))
                {
                    await _projectTeamRepository.AddAsync(newTeam);
                }
            }
        }

        public async Task<IReadOnlyList<IProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode, int? periodId = null)
        {
            IPeriod period = await _periodRepository.GetPeriodAsync(periodId);
            IProject project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
            IReadOnlyList<IProjectTeam> teams = await _projectTeamRepository.GetByProjectWithUsersAsync(project.Id);
            return teams;
        }

        public async Task AddUserToProjectTeamAsync(int courseId, string projectCode, int teamId, int userId)
        {
            IReadOnlyList<IProjectTeam> allTeams = await LoadTeamsOfProjectAsync(courseId, projectCode, periodId: null);
            IProjectTeam currentTeam = allTeams.FirstOrDefault(t => t.TeamUsers.Any(tu => tu.UserId == userId));
            Contracts.Require(currentTeam is null, () => $"The user is already a member of '{currentTeam.Name}'. It is not allowed to be in multiple teams.");

            IProjectTeam targetTeam = allTeams.FirstOrDefault(t => t.Id == teamId);
            Contracts.Require(targetTeam != null, $"The team you want to join is not a team of project '{projectCode}'.");

            await _projectTeamRepository.AddUserToTeam(teamId, userId);
        }

        public async Task RemoveUserFromProjectTeamAsync(int courseId, string projectCode, int teamId, int userId)
        {
            IProject project = await LoadProjectForUserAsync(courseId, projectCode, userId, periodId: null);

            Contracts.Require(project.Teams.Count == 1, "The user is not a member ot the team");

            await _projectTeamRepository.RemoveUserFromTeam(teamId, userId);

            await _testRunRepository.RemoveAllTopicTestRunsOfUserAsync(project.Id, userId);
        }

        public async Task<IReadOnlyList<AssignmentResultDto>> GetResultsForTeamAsync(IProject project, int teamId, DateTime? dateUtc)
        {
            List<AssignmentResultDto> results = new List<AssignmentResultDto>();
            foreach (Assignment assignment in project.Assignments)
            {
                AssignmentResultDto dto = await _assignmentService.GetResultsForTeamAsync(project.Id, assignment.Id, teamId, dateUtc);
                results.Add(dto);
            }

            return results;
        }

        public async Task<IReadOnlyList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(IProject project, DateTime? dateUtc)
        {
            List<AssignmentStatisticsDto> results = new List<AssignmentStatisticsDto>();
            foreach (Assignment assignment in project.Assignments)
            {
                AssignmentStatisticsDto assignmentStatistics =
                    await _assignmentService.GetAssignmentTeamStatisticsAsync(project.Id, assignment.Id, dateUtc);
                results.Add(assignmentStatistics);
            }
            return results;
        }

        public async Task<IReadOnlyList<SolutionDto>> GetAllSolutions(IProject project, DateTime? dateUtc)
        {
            List<SolutionDto> results = new List<SolutionDto>();
            foreach (ProjectTeam team in project.Teams)
            {
                List<SolutionFile> teamFiles = new List<SolutionFile>();
                foreach (Assignment assignment in project.Assignments)
                {
                    IList<SolutionFile> files = await _solutionFileRepository.GetAllLatestOfAssignmentForTeamAsync(assignment.Id, team.Id, dateUtc);
                    teamFiles.AddRange(files);

                }
                results.Add(new SolutionDto
                {
                    WriterId = team.Id,
                    WriterName = team.Name,
                    SolutionFiles = teamFiles
                });
            }
            return results;
        }

        public async Task<IProjectAssessment> CreateProjectAssessmentAsync(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc)
        {
            IProjectAssessment assessment = _assessmentFactory.CreateNew(projectId, description, openOnUtc, deadlineUtc);
            await _projectAssessmentRepository.AddAsync(assessment);
            return assessment;
        }

        public async Task UpdateProjectAssessmentAsync(int id, string description, DateTime openOnUtc, DateTime deadlineUtc)
        {
            IProjectAssessment existingAssessment = await _projectAssessmentRepository.GetByIdAsync(id);
            existingAssessment.Update(description, openOnUtc, deadlineUtc);
            await _projectAssessmentRepository.UpdateAsync(existingAssessment);
        }
    }
}