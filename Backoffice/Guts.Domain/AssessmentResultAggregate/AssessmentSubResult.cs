using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.AssessmentResultAggregate
{
    internal class AssessmentSubResult : IAssessmentSubResult
    {
        public double Value { get; private set; }
        public double SelfValue { get; private set; }
        public double PeerValue { get; private set; }
        public AssessmentScore Score { get; private set; }
        public AssessmentScore SelfScore { get; private set; }
        public AssessmentScore PeerScore { get; private set; }

        public double AverageValue { get; private set; }
        public double AverageSelfValue { get; private set; }
        public double AveragePeerValue { get; private set; }

        internal class Factory : IAssessmentSubResultFactory
        {
            public IAssessmentSubResult Create(int subjectId, IReadOnlyCollection<IPeerAssessment> allPeerAssessments, Func<IPeerAssessment, double> calculateScore)
            {
                AssessmentSubResult result = new AssessmentSubResult();
                result.AverageValue = allPeerAssessments.Sum(pa => calculateScore(pa) / allPeerAssessments.Count);

                List<IPeerAssessment> allSelfAssessments = allPeerAssessments.Where(pa => pa.IsSelfAssessment).ToList();
                result.AverageSelfValue = allSelfAssessments.Sum(pa => calculateScore(pa) / allSelfAssessments.Count);

                List<IPeerAssessment> allNonSelfAssessments = allPeerAssessments.Where(pa => !pa.IsSelfAssessment).ToList();
                result.AveragePeerValue = allNonSelfAssessments.Sum(pa => calculateScore(pa) / allNonSelfAssessments.Count);

                IPeerAssessment selfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.IsSelfAssessment && pa.Subject.Id == subjectId);
                Contracts.Require(selfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

                List<IPeerAssessment> peerAssessmentsForSubject = allPeerAssessments.Where(pa => pa.Subject.Id == subjectId && pa.User.Id != subjectId).ToList();
                Contracts.Require(peerAssessmentsForSubject.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

                result.SelfValue = calculateScore(selfAssessment);

                result.PeerValue = peerAssessmentsForSubject.Sum(pa => calculateScore(pa) / peerAssessmentsForSubject.Count);

                List<IPeerAssessment> allPeerAssessmentsForSubject = new List<IPeerAssessment>(peerAssessmentsForSubject) { selfAssessment };
                result.Value = allPeerAssessmentsForSubject.Sum(pa => calculateScore(pa) / allPeerAssessmentsForSubject.Count);

                result.SelfScore = result.SelfValue;
                result.PeerScore = result.PeerValue;
                result.Score = result.Value;

                return result;
            }
        }

        private AssessmentSubResult() { }
    }
}