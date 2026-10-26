using System.Diagnostics.CodeAnalysis;
using System;
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

namespace Guts.Business.Services;

[method: SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Service orchestrates project domain dependencies; preserving backward compatibility.")]
internal class ProjectService(
    IProjectRepository projectRepository,
    ICourseRepository courseRepository,
    IPeriodRepository periodRepository,
    ITopicRepository topicRepository,
    IProjectTeamRepository projectTeamRepository,
    ISolutionFileRepository solutionFileRepository,
    IAssignmentService assignmentService,
    IProjectAssessmentRepository projectAssessmentRepository,
    IProjectAssessmentFactory assessmentFactory,
    ITestRunRepository testRunRepository)
    : IProjectService
{
    public async Task<IProject> GetOrCreateProjectAsync(string courseCode, string projectCode, int? periodId = null)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(periodId);

        IProject project;
        try
        {
            project = await projectRepository.GetSingleAsync(courseCode, projectCode, period.Id);
        }
        catch (DataNotFoundException)
        {
            Course course = await courseRepository.GetSingleAsync(courseCode);
            project = new Project
            {
                Code = projectCode,
                CourseId = course.Id,
                PeriodId = period.Id,
                Description = projectCode
            };
            project = await projectRepository.AddAsync(project);
        }

        return project;
    }

    public async Task<IProject> CreateProjectAsync(int courseId, Code projectCode,
        string description)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(null);

        IProject project;
        try
        {
            await projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
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
            project = await projectRepository.AddAsync(project);
        }
        return project;
    }

    public async Task UpdateProjectAsync(int courseId, string projectCode, string description)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(null);

        await topicRepository.UpdateAsync(courseId, projectCode, period.Id, description);
    }

    public async Task<IReadOnlyList<IProject>> GetProjectsOfCourseAsync(int courseId, int? periodId = null)
    {
        try
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);
            IReadOnlyList<IProject> projects = await projectRepository.GetByCourseIdAsync(courseId, period.Id);
            return [.. projects.OrderBy(p => p.Description)];
        }
        catch (DataNotFoundException)
        {
            return new List<Project>();
        }
    }

    public async Task<IProject> LoadProjectAsync(int courseId, string projectCode, int? periodId = null)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(periodId);

        IProject project = await projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);
        return project;
    }

    public async Task<IProject> LoadProjectForUserAsync(int courseId, string projectCode, int userId, int? periodId = null)
    {
        Contracts.Require(courseId > 0, "Invalid course identifier");
        Contracts.Require(!string.IsNullOrEmpty(projectCode), "Project code cannot be empty");
        Contracts.Require(userId > 0, "Invalid user id");

        IPeriod period = await periodRepository.GetPeriodAsync(periodId);
        IProject project = await projectRepository.LoadWithAssignmentsAndTeamsOfUserAsync(courseId, projectCode, period.Id, userId);
        return project;
    }

    public async Task<IProjectTeam> AddProjectTeamAsync(int courseId, string projectCode, string teamName)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(null);

        IProject project = await projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);

        ProjectTeam newTeam = new()
        {
            Name = teamName,
            ProjectId = project.Id
        };

        ICollection<IProjectTeam> allTeams = project.Teams;
        bool alreadyExists = allTeams.Any(team => string.Equals(team.Name, newTeam.Name, StringComparison.OrdinalIgnoreCase));
        Contracts.Require(!alreadyExists, "A team with the same name already exists");

        return await projectTeamRepository.AddAsync(newTeam);
    }

    public async Task UpdateProjectTeamAsync(int courseId, string projectCode, int teamId, string teamName)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(null);
        IProject project = await projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
        IProjectTeam teamToUpdate = await projectTeamRepository.LoadByIdAsync(teamId);

        Contracts.Require(teamToUpdate.ProjectId == project.Id, "The team does not belong to the project");

        teamToUpdate.Name = teamName;

        await projectTeamRepository.UpdateAsync(teamToUpdate);
    }

    public async Task DeleteProjectTeamAsync(int courseId, string projectCode, int teamId)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(null);
        IProject project = await projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
        IProjectTeam teamToDelete = await projectTeamRepository.LoadByIdAsync(teamId);

        Contracts.Require(teamToDelete.ProjectId == project.Id, "The team does not belong to the project");

        await projectTeamRepository.DeleteAsync(teamToDelete);
    }

    public async Task GenerateTeamsForProject(int courseId, string projectCode, string teamBaseName, int teamNumberFrom, int teamNumberTo)
    {
        Contracts.Require(teamNumberFrom > 0, "The start team number must be greater than zero.");
        Contracts.Require(teamNumberTo >= teamNumberFrom, "The end team number must be greater than or equal to the start team number.");
        Contracts.Require(teamNumberTo - teamNumberFrom <= 100, "Cannot generate more than 100 teams in a single request.");
        IPeriod period = await periodRepository.GetPeriodAsync(null);

        IProject project = await projectRepository.LoadWithAssignmentsAndTeamsAsync(courseId, projectCode, period.Id);

        ICollection<IProjectTeam> allTeams = project.Teams;

        for (int teamNumber = teamNumberFrom; teamNumber <= teamNumberTo; teamNumber++)
        {
            ProjectTeam newTeam = new()
            {
                Name = $"{teamBaseName} {teamNumber:D2}",
                ProjectId = project.Id
            };

            if (allTeams.All(team => !string.Equals(team.Name, newTeam.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await projectTeamRepository.AddAsync(newTeam);
            }
        }
    }

    public async Task<IReadOnlyList<IProjectTeam>> LoadTeamsOfProjectAsync(int courseId, string projectCode, int? periodId = null)
    {
        IPeriod period = await periodRepository.GetPeriodAsync(periodId);
        IProject project = await projectRepository.GetSingleAsync(courseId, projectCode, period.Id);
        IReadOnlyList<IProjectTeam> teams = await projectTeamRepository.GetByProjectWithUsersAsync(project.Id);
        return teams;
    }

    public async Task AddUserToProjectTeamAsync(int courseId, string projectCode, int teamId, int userId)
    {
        IReadOnlyList<IProjectTeam> allTeams = await LoadTeamsOfProjectAsync(courseId, projectCode, periodId: null);
        IProjectTeam currentTeam = allTeams.FirstOrDefault(t => t.TeamUsers.Any(tu => tu.UserId == userId));
        Contracts.Require(currentTeam is null, () => $"The user is already a member of '{currentTeam.Name}'. It is not allowed to be in multiple teams.");

        IProjectTeam targetTeam = allTeams.FirstOrDefault(t => t.Id == teamId);
        Contracts.Require(targetTeam != null, $"The team you want to join is not a team of project '{projectCode}'.");

        await projectTeamRepository.AddUserToTeam(teamId, userId);
    }

    public async Task RemoveUserFromProjectTeamAsync(int courseId, string projectCode, int teamId, int userId)
    {
        IProject project = await LoadProjectForUserAsync(courseId, projectCode, userId, periodId: null);

        Contracts.Require(project.Teams.Count == 1, "The user is not a member ot the team");

        await projectTeamRepository.RemoveUserFromTeam(teamId, userId);

        await testRunRepository.RemoveAllTopicTestRunsOfUserAsync(project.Id, userId);
    }

    public async Task<IReadOnlyList<AssignmentResultDto>> GetResultsForTeamAsync(IProject project, int teamId, DateTime? dateUtc)
    {
        List<AssignmentResultDto> results = [];
        foreach (Assignment assignment in project.Assignments)
        {
            AssignmentResultDto dto = await assignmentService.GetResultsForTeamAsync(project.Id, assignment.Id, teamId, dateUtc);
            results.Add(dto);
        }

        return results;
    }

    public async Task<IReadOnlyList<AssignmentStatisticsDto>> GetProjectStatisticsAsync(IProject project, DateTime? dateUtc)
    {
        List<AssignmentStatisticsDto> results = [];
        foreach (Assignment assignment in project.Assignments)
        {
            AssignmentStatisticsDto assignmentStatistics =
                await assignmentService.GetAssignmentTeamStatisticsAsync(project.Id, assignment.Id, dateUtc);
            results.Add(assignmentStatistics);
        }
        return results;
    }

    public async Task<IReadOnlyList<SolutionDto>> GetAllSolutions(IProject project, DateTime? dateUtc)
    {
        List<SolutionDto> results = [];
        foreach (IProjectTeam team in project.Teams)
        {
            List<SolutionFile> teamFiles = [];
            foreach (Assignment assignment in project.Assignments)
            {
                IList<SolutionFile> files = await solutionFileRepository.GetAllLatestOfAssignmentForTeamAsync(assignment.Id, team.Id, dateUtc);
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
        IProjectAssessment assessment = assessmentFactory.CreateNew(projectId, description, openOnUtc, deadlineUtc);
        await projectAssessmentRepository.AddAsync(assessment);
        return assessment;
    }

    public async Task UpdateProjectAssessmentAsync(int id, string description, DateTime openOnUtc, DateTime deadlineUtc)
    {
        IProjectAssessment existingAssessment = await projectAssessmentRepository.GetByIdAsync(id);
        existingAssessment.Update(description, openOnUtc, deadlineUtc);
        await projectAssessmentRepository.UpdateAsync(existingAssessment);
    }
}