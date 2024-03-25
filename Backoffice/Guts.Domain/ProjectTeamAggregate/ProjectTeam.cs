using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Guts.Common;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public class ProjectTeam : AggregateRoot, IProjectTeam
    {
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                Contracts.Require(!string.IsNullOrWhiteSpace(value), "The team name is required.");
                _name = value;
            }
        }

        public IProject Project { get; set; }
        public int ProjectId { get; set; }

        public ICollection<IProjectTeamUser> TeamUsers { get; set; } = new HashSet<IProjectTeamUser>();
        public User GetTeamUser(int userId)
        {
            IProjectTeamUser teamUser = TeamUsers.SingleOrDefault(tu => tu.UserId == userId);
            Contracts.Require(teamUser != null, $"The user with id '{userId}' is not a member of the team.");
            return teamUser!.User;
        }
    }
}