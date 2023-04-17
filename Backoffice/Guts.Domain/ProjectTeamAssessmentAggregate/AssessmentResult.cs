using System;
using Guts.Common;
using Guts.Domain.UserAggregate;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class AssessmentResult : IAssessmentResult
    {
        public User Subject { get; private set; }
        public IPeerAssessment SelfAssessment { get; private set; }
        public IReadOnlyList<IPeerAssessment> PeerAssessments { get; private set; }
        public IAssessmentSubResult AverageResult { get; private set; }
        public IAssessmentSubResult EffortResult { get; private set; }
        public IAssessmentSubResult CooperationResult { get; private set; }
        public IAssessmentSubResult ContributionResult { get; private set; }

        internal class Factory : IAssessmentResultFactory
        {
            private readonly IAssessmentSubResultFactory _subResultFactory;

            public Factory(IAssessmentSubResultFactory subResultFactory)
            {
                _subResultFactory = subResultFactory;
            }
            public IAssessmentResult Create(User subject, IReadOnlyCollection<IPeerAssessment> allPeerAssessments)
            {
                AssessmentResult result = new AssessmentResult();

                Contracts.Require(subject != null, "An assessment result needs a subject.");
                result.Subject = subject;

                result.SelfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.Subject.Id == subject.Id);
                Contracts.Require(result.SelfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

                result.PeerAssessments = allPeerAssessments.Where(pa => pa.Subject.Id == subject.Id && pa.User.Id != subject.Id).ToList();
                Contracts.Require(result.PeerAssessments.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");

                result.AverageResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => (pa.ContributionScore + pa.CooperationScore + pa.EffortScore) / 3.0);
                result.EffortResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.EffortScore);
                result.ContributionResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.ContributionScore);
                result.CooperationResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.CooperationScore);

                return result;
            }
        }

        private AssessmentResult(){}
    }
}
