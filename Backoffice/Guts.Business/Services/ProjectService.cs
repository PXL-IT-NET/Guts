﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
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
        private readonly IProjectTeamRepository _projectTeamRepository;
        private readonly ISolutionFileRepository _solutionFileRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly IProjectAssessmentRepository _projectAssessmentRepository;
        private readonly IProjectAssessmentFactory _assessmentFactory;

        public ProjectService(IProjectRepository projectRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository,
            IProjectTeamRepository projectTeamRepository,
            ISolutionFileRepository solutionFileRepository,
            IAssignmentService assignmentService,
            IProjectAssessmentRepository projectAssessmentRepository,
            IProjectAssessmentFactory assessmentFactory)
        {
            _projectRepository = projectRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _projectTeamRepository = projectTeamRepository;
            _solutionFileRepository = solutionFileRepository;
            _assignmentService = assignmentService;
            _projectAssessmentRepository = projectAssessmentRepository;
            _assessmentFactory = assessmentFactory;
        }

        //public async Task<IProject> GetProjectAsync(string courseCode, string projectCode)
        //{
        //    var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
        //    return await _projectRepository.GetSingleAsync(courseCode, projectCode, currentPeriod.Id);
        //}

        public async Task<IProject> GetOrCreateProjectAsync(string courseCode, string projectCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            IProject project;
            try
            {
                project = await _projectRepository.GetSingleAsync(courseCode, projectCode, currentPeriod.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await _courseRepository.GetSingleAsync(courseCode);
                project = new Project
                {
                    Code = projectCode,
                    CourseId = course.Id,
                    PeriodId = currentPeriod.Id,
                    Description = string.Empty
                };
                project = await _projectRepository.AddAsync(project);
            }

            return project;
        }

        public async Task<IReadOnlyList<IProject>> GetProjectsOfCourseAsync(int courseId)
        {
            try
            {
                var period = await _periodRepository.GetCurrentPeriodAsync();
                var projects = await _projectRepository.GetByCourseIdAsync(courseId, period.Id);
                return projects.OrderBy(p => p.Description).ToList();
            }
            catch (DataNotFoundException)
            {
                return new List<Project>();
            }
        }

        public async Task<IProject> LoadProjectAsync(int courseId, string projectCode)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);
            return project;
        }

        public async Task<IProject> LoadProjectForUserAsync(int courseId, string projectCode, int userId)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.LoadWithAssignmentsAndTeamsOfUserAsync(courseId, projectCode, period.Id, userId);
            return project;
        }

        public async Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int numberOfTeams)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);

            if (project.Teams.Count >= numberOfTeams) return;

            for (int teamNumber = project.Teams.Count + 1; teamNumber <= numberOfTeams; teamNumber++)
            {
                var newTeam = new ProjectTeam
                {
                    Name = $"{teamBaseName} {teamNumber}",
                    ProjectId = project.Id
                };
                await _projectTeamRepository.AddAsync(newTeam);
            }
        }

        public async Task<IReadOnlyList<IProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
            var teams = await _projectTeamRepository.GetByProjectWithUsersAsync(project.Id);
            return teams;
        }

        public async Task AddUserToProjectTeamAsync(int teamId, int userId)
        {
            await _projectTeamRepository.AddUserToTeam(teamId, userId);
        }

        public async Task<IReadOnlyList<AssignmentResultDto>> GetResultsForTeamAsync(IProject project, int teamId, DateTime? dateUtc)
        {
            var results = new List<AssignmentResultDto>();
            foreach (var assignment in project.Assignments)
            {
                var dto = await _assignmentService.GetResultsForTeamAsync(assignment.Id, teamId, dateUtc);
                results.Add(dto);
            }

            return results;
        }

        public async Task<IReadOnlyList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(IProject project, DateTime? dateUtc)
        {
            var results = new List<AssignmentStatisticsDto>();
            foreach (var assignment in project.Assignments)
            {
                var assignmentStatistics =
                    await _assignmentService.GetAssignmentTeamStatisticsAsync(assignment.Id, dateUtc);
                results.Add(assignmentStatistics);
            }
            return results;
        }

        public async Task<IReadOnlyList<SolutionDto>> GetAllSolutions(IProject project, DateTime? dateUtc)
        {
            var results = new List<SolutionDto>();
            foreach (ProjectTeam team in project.Teams)
            {
                var teamFiles = new List<SolutionFile>();
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

        public async Task<IReadOnlyList<IProjectAssessment>> GetProjectAssessmentsAsync(int projectId)
        {
            return await _projectAssessmentRepository.FindByProjectIdAsync(projectId);
        }

        public async Task<IProjectAssessment> CreateProjectAssessmentAsync(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc)
        {
            IProjectAssessment assessment = _assessmentFactory.CreateNew(projectId, description, openOnUtc, deadlineUtc);
            await _projectAssessmentRepository.AddAsync(assessment);
            return assessment;
        }
    }
}