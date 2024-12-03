using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    public interface IProjectService
    {
        Task<IProject> GetOrCreateProjectAsync(string courseCode, string projectCode, int? periodId = null);
        Task<IProject> CreateProjectAsync(int courseId, Code projectCode, string description);
        Task UpdateProjectAsync(int courseId, string projectCode, string description);

        Task<IReadOnlyList<IProject>> GetProjectsOfCourseAsync(int courseId, int? periodId = null);

        /// <summary>
        /// Loads a project with its assignments and all teams
        /// </summary>
        Task<IProject> LoadProjectAsync(int courseId, string projectCode, int? periodId = null);

        /// <summary>
        /// Loads a project with its assignments and only the team(s) of the user
        /// </summary>
        Task<IProject> LoadProjectForUserAsync(int courseId, string projectCode, int userId, int? periodId = null);

        Task<IProjectTeam> AddProjectTeamAsync(int courseId, string projectCode, string teamName);

        Task UpdateProjectTeamAsync(int courseId, string projectCode, int teamId, string teamName);

        Task DeleteProjectTeamAsync(int courseId, string projectCode, int teamId);

        Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int teamNumberFrom, int teamNumberTo);

        Task<IReadOnlyList<IProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode, int? periodId = null);

        Task AddUserToProjectTeamAsync(int courseId, string projectCode, int teamId, int userId);

        Task RemoveUserFromProjectTeamAsync(int courseId, string projectCode, int teamId, int userId);

        Task<IReadOnlyList<AssignmentResultDto>>
            GetResultsForTeamAsync(IProject project, int teamId, DateTime? dateUtc);

        Task<IReadOnlyList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(IProject project, DateTime? dateUtc);

        Task<IReadOnlyList<SolutionDto>> GetAllSolutions(IProject project, DateTime? dateUtc);

        Task<IProjectAssessment> CreateProjectAssessmentAsync(int projectId, string description, DateTime openOnUtc,
            DateTime deadlineUtc);

        Task UpdateProjectAssessmentAsync(int id, string description, DateTime openOnUtc,
            DateTime deadlineUtc);
    }
}