using Guts.Domain.ProjectTeamAggregate;
using System.Collections.Generic;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public interface IProject : ITopic
    {
        ICollection<IProjectAssessment> Assessments { get; set; }
        ICollection<ProjectTeam> Teams { get; set; }
    }
}