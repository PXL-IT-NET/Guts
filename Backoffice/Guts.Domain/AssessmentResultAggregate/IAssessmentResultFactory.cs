using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Domain.AssessmentResultAggregate
{
    public interface IAssessmentResultFactory
    {
        IAssessmentResult Create(int subjectId, IProjectTeamAssessment projectTeamAssessment,
            bool includePeerAssessments);
    }
}