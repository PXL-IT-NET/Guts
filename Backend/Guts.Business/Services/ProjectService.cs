using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IProjectTeamRepository _projectTeamRepository;
        private readonly ITestResultRepository _testResultRepository;

        public ProjectService(IProjectRepository projectRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository, 
            IProjectTeamRepository projectTeamRepository,
            ITestResultRepository testResultRepository)
        {
            _projectRepository = projectRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _projectTeamRepository = projectTeamRepository;
            _testResultRepository = testResultRepository;
        }

        public async Task<Project> GetProjectAsync(string courseCode, string projectCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _projectRepository.GetSingleAsync(courseCode, projectCode, currentPeriod.Id);
        }

        public async Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            Project project;
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

        public async Task<IList<Project>> GetProjectsOfCourseAsync(int courseId)
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

        public async Task<Project> LoadProjectAsync(int courseId, string projectCode)
        {
                var period = await _periodRepository.GetCurrentPeriodAsync();
                var project = await _projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);
                return project;
        }

        public async Task<Project> LoadProjectForUserAsync(int courseId, string projectCode, int userId)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.LoadWithAssignmentsAndTeamsOfUserAsync(courseId, projectCode, period.Id, userId);
            return project;
        }

        public async Task<IList<ProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode)
        {
            var period = await _periodRepository.GetCurrentPeriodAsync();
            var project = await _projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
            var teams = await _projectTeamRepository.GetByProjectWithUsersAsync(project.Id);
            return teams;
        }

        public async Task AddUserToProjectTeam(int teamId, int userId)
        {
            await _projectTeamRepository.AddUserToTeam(teamId, userId);
        }

        public async Task<IList<AssignmentResultDto>> GetResultsForTeamAsync(Project project, int teamId, DateTime? dateUtc)
        {
            //TOOD: write tests
            if (project.Assignments == null)
            {
                throw new ArgumentException("The project should have its assignments loaded");
            }

            var results = new List<AssignmentResultDto>();
            foreach (var assignment in project.Assignments)
            {
                var dto = new AssignmentResultDto
                {
                    AssignmentId = assignment.Id,
                    TestResults = await _testResultRepository.GetLastTestResultsOfTeam(assignment.Id, teamId, dateUtc)
                };
                results.Add(dto);
            }

            return results;
        }

        public Task<IList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(Project project, DateTime? dateUtc)
        {
            throw new NotImplementedException();
        }
    }
}