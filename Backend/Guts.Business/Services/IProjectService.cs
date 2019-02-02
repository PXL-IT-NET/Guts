using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

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
        /// Loads a project with ists assignments and only the team(s) of the user
        /// </summary>
        Task<Project> LoadProjectForUserAsync(int courseId, string projectCode, int userId);

        Task<IList<ProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode);

        Task AddUserToProjectTeam(int teamId, int userId);
    }
}