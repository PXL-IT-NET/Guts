using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IProjectTeamAssessmentFactory
    {
        IProjectTeamAssessment CreateNew(IProjectAssessment projectAssessment, IProjectTeam team);
    }
}