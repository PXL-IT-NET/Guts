using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate;

public interface IProjectTeamUser : IEntity
{
    int Id { get; }
    IProjectTeam ProjectTeam { get; set; }
    int ProjectTeamId { get; set; }
    User User { get; set; }
    int UserId { get; set; }
}