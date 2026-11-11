using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate;

public interface IProjectTeamAssessment : IEntity
{
    int Id { get; }

    IProjectAssessment ProjectAssessment { get; }

    IProjectTeam Team { get; }

    IReadOnlyCollection<User> Peers { get; }

    IReadOnlyCollection<IPeerAssessment> PeerAssessments { get; }

    bool IsComplete { get; }

    User GetPeer(int userId);

    void AddPeer(int userId);

    IReadOnlyList<IPeerAssessment> RemovePeer(int userId);

    void SetPeerInclusion(int userId, bool isIncluded);

    IPeerAssessment AddOrUpdatePeerAssessment(int userId, int subjectId,
        AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore, 
        string explanation);

    IReadOnlyList<IPeerAssessment> GetPeerAssessmentsOf(int userId);

    IReadOnlyList<IPeerAssessment> GetMissingPeerAssessmentsOf(int userId);

    IReadOnlyList<User> GetPeersThatNeedToEvaluateOthers();

    void ValidateAssessmentsOf(int userId);
}