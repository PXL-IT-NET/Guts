using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using System.Collections.Generic;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IProjectTeamAssessment: IEntity
    {
        IProjectAssessment ProjectAssessment { get; }

        IProjectTeam Team { get; }

        IReadOnlyCollection<IPeerAssessment> PeerAssessments { get; }

        bool IsComplete { get; }

        void AddOrReplacePeerAssessment(int userId, int subjectId,
            AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore);

        IReadOnlyList<User> GetPeersToEvaluateFor(int userId);
    }
}