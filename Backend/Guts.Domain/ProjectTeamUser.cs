namespace Guts.Domain
{
    public class ProjectTeamUser : IDomainObject
    {
        public int Id { get; set; }

        public virtual ProjectTeam ProjectTeam { get; set; }
        public int ProjectTeamId { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }      
    }
}