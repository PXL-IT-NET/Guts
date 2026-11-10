using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate;

public interface IProject : ITopic
{
    ICollection<IProjectAssessment> Assessments { get; set; }
    ICollection<IProjectTeam> Teams { get; set; }

    bool HasTeam(string teamName);
    IProjectTeam AddTeam(string teamName);
    void AddTeam(IProjectTeam team);
    IReadOnlyList<IProjectTeam> GenerateTeams(string teamBaseName, int teamNumberFrom, int teamNumberTo);
    IProjectTeam FindTeamOfUser(int userId);
    void ValidateUserCanJoinTeam(int teamId, int userId);
    void EnsureTeamBelongsToProject(IProjectTeam team);
    void EnsureUserIsMemberOfProject(int userId);
    void LoadTeams(IEnumerable<IProjectTeam> teams);
    void ClearTeams();
}