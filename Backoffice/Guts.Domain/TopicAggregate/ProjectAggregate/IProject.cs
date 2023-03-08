using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public interface IProject : ITopic
    {
        ICollection<IProjectAssessment> Assessments { get; set; }
        ICollection<ProjectTeam> Teams { get; set; }
    }
}