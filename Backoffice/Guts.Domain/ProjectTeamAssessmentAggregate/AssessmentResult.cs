using System;
using Guts.Common;
using Guts.Domain.UserAggregate;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class AssessmentResult : IAssessmentResult
    {
        public User Subject { get; }
        public IPeerAssessment SelfAssessment { get; }
        public IReadOnlyList<IPeerAssessment> PeerAssessments { get; }
        public IAssessmentSubResult AverageResult { get; }
        public IAssessmentSubResult EffortResult { get; }
        public IAssessmentSubResult CooperationResult { get; }
        public IAssessmentSubResult ContributionResult { get; }

        public AssessmentResult(User subject, IReadOnlyCollection<IPeerAssessment> allPeerAssessments)
        {
            Contracts.Require(subject != null, "An assessment result needs a subject.");
            Subject = subject;

            SelfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.Subject.Id == subject.Id);
            Contracts.Require(SelfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

            PeerAssessments = allPeerAssessments.Where(pa => pa.Subject.Id == subject.Id && pa.User.Id != subject.Id).ToList();
            Contracts.Require(PeerAssessments.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

            AverageResult = new AssessmentSubResult(Subject.Id, allPeerAssessments, pa => (pa.ContributionScore + pa.CooperationScore + pa.EffortScore) / 3.0);
            EffortResult = new AssessmentSubResult(Subject.Id, allPeerAssessments, pa => pa.EffortScore);
            ContributionResult = new AssessmentSubResult(Subject.Id, allPeerAssessments, pa => pa.ContributionScore);
            CooperationResult = new AssessmentSubResult(Subject.Id, allPeerAssessments, pa => pa.CooperationScore);
        }
    }
}
