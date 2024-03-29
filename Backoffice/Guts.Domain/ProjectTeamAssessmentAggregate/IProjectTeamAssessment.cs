﻿using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IProjectTeamAssessment : IEntity
    {
        IProjectAssessment ProjectAssessment { get; }

        IProjectTeam Team { get; }

        IReadOnlyCollection<IPeerAssessment> PeerAssessments { get; }

        bool IsComplete { get; }

        IPeerAssessment AddOrUpdatePeerAssessment(int userId, int subjectId,
            AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore, 
            string explanation);

        IReadOnlyList<IPeerAssessment> GetPeerAssessmentsOf(int userId);

        IReadOnlyList<IPeerAssessment> GetMissingPeerAssessmentsOf(int userId);

        IReadOnlyList<User> GetPeersThatNeedToEvaluateOthers();

        void ValidateAssessmentsOf(int userId);
    }
}