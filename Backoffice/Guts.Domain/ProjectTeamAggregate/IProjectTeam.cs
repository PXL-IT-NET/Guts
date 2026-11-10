using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate;

public interface IProjectTeam : IEntity
{
    string Name { get; set; }
    IProject Project { get; }
    int ProjectId { get; }
    ICollection<IProjectTeamUser> TeamUsers { get; set; }

    User GetTeamUser(int userId);
    bool HasMember(int userId);
    void AddUser(int userId);
    void AddUser(User user);
    void RemoveUser(int userId);
    void Rename(string newName);
    void EnsureBelongsTo(int courseId, string projectCode);
}