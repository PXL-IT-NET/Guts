using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public class ProjectTeamUser : Entity
    {
        public virtual ProjectTeam ProjectTeam { get; set; }
        public int ProjectTeamId { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }      
    }
}