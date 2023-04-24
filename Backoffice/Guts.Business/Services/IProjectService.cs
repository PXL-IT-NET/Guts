using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Business.Services
{
    public interface IProjectService
    {
        Task<IProject> GetOrCreateProjectAsync(string courseCode, string projectCode);
        Task<IReadOnlyList<IProject>> GetProjectsOfCourseAsync(int courseId);

        /// <summary>
        /// Loads a project with its assignments and all teams
        /// </summary>
        Task<IProject> LoadProjectAsync(int courseId, string projectCode);

        /// <summary>
        /// Loads a project with its assignments and only the team(s) of the user
        /// </summary>
        Task<IProject> LoadProjectForUserAsync(int courseId, string projectCode, int userId);

        Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int teamNumberFrom, int teamNumberTo);

        Task<IReadOnlyList<IProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode);

        Task AddUserToProjectTeamAsync(int teamId, int userId);

        Task RemoveUserFromProjectTeamAsync(int courseId, string projectCode, int teamId, int userId);

        Task<IReadOnlyList<AssignmentResultDto>>
            GetResultsForTeamAsync(IProject project, int teamId, DateTime? dateUtc);

        Task<IReadOnlyList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(IProject project, DateTime? dateUtc);

        Task<IReadOnlyList<SolutionDto>> GetAllSolutions(IProject project, DateTime? dateUtc);

        Task<IProjectAssessment> CreateProjectAssessmentAsync(int projectId, string description, DateTime openOnUtc,
            DateTime deadlineUtc);
    }
}