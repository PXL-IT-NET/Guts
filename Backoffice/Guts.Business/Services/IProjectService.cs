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
        Task<Project> GetProjectAsync(string courseCode, string projectCode);
        Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode);
        Task<IList<Project>> GetProjectsOfCourseAsync(int courseId);

        /// <summary>
        /// Loads a project with its assignments and all teams
        /// </summary>
        Task<Project> LoadProjectAsync(int courseId, string projectCode);

        /// <summary>
        /// Loads a project with its assignments and only the team(s) of the user
        /// </summary>
        Task<Project> LoadProjectForUserAsync(int courseId, string projectCode, int userId);

        Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int numberOfTeams);

        Task<IList<ProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode);

        Task AddUserToProjectTeamAsync(int teamId, int userId);

        Task<IList<AssignmentResultDto>> GetResultsForTeamAsync(Project project, int teamId, DateTime? dateUtc);

        Task<IList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(Project project, DateTime? dateUtc);

        Task<IList<SolutionDto>> GetAllSolutions(Project project, DateTime? dateUtc);
    }
}