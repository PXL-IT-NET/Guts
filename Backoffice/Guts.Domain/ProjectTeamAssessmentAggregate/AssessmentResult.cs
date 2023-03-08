using Guts.Common;
using Guts.Domain.UserAggregate;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class AssessmentResult : IAssessmentResult
    {
        public User Subject { get; }

        public IPeerAssessment SelfAssessment { get; }

        public IReadOnlyList<IPeerAssessment> PeerAssessments { get; }

        public AssessmentScore SelfAssessmentScore { get; }

        public AssessmentScore PeerAssessmentScore { get; }

        public double SelfAssessmentFactor { get; }

        public double PeerAssessmentFactor { get; }

        public AssessmentResult(User subject, IReadOnlyCollection<IPeerAssessment> allPeerAssessments)
        {
            Contracts.Require(subject != null, "An assessment result needs a subject.");
            Subject = subject;

            SelfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.Subject.Id == subject.Id);
            Contracts.Require(SelfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

            PeerAssessments = allPeerAssessments.Where(pa => pa.Subject.Id == subject.Id && pa.User.Id != subject.Id).ToList();
            Contracts.Require(PeerAssessments.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

            double selfAssessmentAverage = (SelfAssessment.ContributionScore + SelfAssessment.CooperationScore + SelfAssessment.EffortScore) / 3.0;

            SelfAssessmentFactor = selfAssessmentAverage / AssessmentScore.Average;

            double peerAssessmentAverage = allPeerAssessments.Sum(pa => pa.ContributionScore + pa.CooperationScore + pa.EffortScore) / (3.0 * allPeerAssessments.Count);

            PeerAssessmentFactor = peerAssessmentAverage / AssessmentScore.Average;

            SelfAssessmentScore = selfAssessmentAverage;

            PeerAssessmentScore = peerAssessmentAverage;
        }
    }
}
