using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class AssessmentSubResult : IAssessmentSubResult
    {
        public double Average { get; }
        public double TeamAverage { get; }
        public double SelfAverage { get; }
        public double PeerAverage { get; }

        public AssessmentScore Score { get; }
        public AssessmentScore SelfScore { get; }
        public AssessmentScore PeerScore { get; }

        public AssessmentSubResult(int subjectId, IReadOnlyCollection<IPeerAssessment> allPeerAssessments, Func<IPeerAssessment, double> calculateScore)
        {
            IPeerAssessment selfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.Subject.Id == subjectId);
            Contracts.Require(selfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

            List<IPeerAssessment> peerAssessmentsForSubject = allPeerAssessments.Where(pa => pa.Subject.Id == subjectId && pa.User.Id != subjectId).ToList();
            Contracts.Require(peerAssessmentsForSubject.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

            TeamAverage = allPeerAssessments.Sum(pa => calculateScore(pa) / allPeerAssessments.Count);

            SelfAverage = calculateScore(selfAssessment);

            PeerAverage = peerAssessmentsForSubject.Sum(pa => calculateScore(pa) / peerAssessmentsForSubject.Count);

            List<IPeerAssessment> allPeerAssessmentsForSubject = new List<IPeerAssessment>(peerAssessmentsForSubject) { selfAssessment };
            Average = allPeerAssessmentsForSubject.Sum(pa => calculateScore(pa) / allPeerAssessmentsForSubject.Count);

            SelfScore = SelfAverage;
            PeerScore = PeerAverage;
            Score = Average;
        }
    }
}