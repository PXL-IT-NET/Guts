using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public class Project : Topic, IProject
    {
        public Project()
        {
        }

        public Project(int id) : base(id)
        {
        }

        public ICollection<IProjectAssessment> Assessments { get; set; } = new HashSet<IProjectAssessment>();
        public ICollection<IProjectTeam> Teams { get; set; } = new HashSet<IProjectTeam>();
    }
}