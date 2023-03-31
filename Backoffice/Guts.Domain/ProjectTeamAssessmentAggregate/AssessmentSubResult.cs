using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class AssessmentSubResult : IAssessmentSubResult
    {
        public double Value { get; }
        public double SelfValue { get; }
        public double PeerValue { get; }
        public AssessmentScore Score { get; }
        public AssessmentScore SelfScore { get; }
        public AssessmentScore PeerScore { get; }

        public double AverageValue { get; }
        public double AverageSelfValue { get; }
        public double AveragePeerValue { get; }

        public AssessmentSubResult(int subjectId, IReadOnlyCollection<IPeerAssessment> allPeerAssessments, Func<IPeerAssessment, double> calculateScore)
        {
            AverageValue = allPeerAssessments.Sum(pa => calculateScore(pa) / allPeerAssessments.Count);

            List<IPeerAssessment> allSelfAssessments = allPeerAssessments.Where(pa => pa.IsSelfAssessment).ToList();
            AverageSelfValue = allSelfAssessments.Sum(pa => calculateScore(pa) / allSelfAssessments.Count);

            List<IPeerAssessment> allNonSelfAssessments = allPeerAssessments.Where(pa => !pa.IsSelfAssessment).ToList();
            AveragePeerValue = allNonSelfAssessments.Sum(pa => calculateScore(pa) / allNonSelfAssessments.Count);

            IPeerAssessment selfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.Subject.Id == subjectId);
            Contracts.Require(selfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

            List<IPeerAssessment> peerAssessmentsForSubject = allPeerAssessments.Where(pa => pa.Subject.Id == subjectId && pa.User.Id != subjectId).ToList();
            Contracts.Require(peerAssessmentsForSubject.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

            SelfValue = calculateScore(selfAssessment);

            PeerValue = peerAssessmentsForSubject.Sum(pa => calculateScore(pa) / peerAssessmentsForSubject.Count);

            List<IPeerAssessment> allPeerAssessmentsForSubject = new List<IPeerAssessment>(peerAssessmentsForSubject) { selfAssessment };
            Value = allPeerAssessmentsForSubject.Sum(pa => calculateScore(pa) / allPeerAssessmentsForSubject.Count);

            SelfScore = SelfValue;
            PeerScore = PeerValue;
            Score = Value;
        }
    }
}