using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.AssessmentResultAggregate
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

        private AssessmentResult()
        {
            PeerAssessments = new List<IPeerAssessment>();
        }

        internal class Factory : IAssessmentResultFactory
        {
            private readonly IAssessmentSubResultFactory _subResultFactory;

            public Factory(IAssessmentSubResultFactory subResultFactory)
            {
                _subResultFactory = subResultFactory;
            }
            public IAssessmentResult Create(int subjectId, IProjectTeamAssessment projectTeamAssessment, bool includePeerAssessments)
            {
                AssessmentResult result = new AssessmentResult();

                User subject = projectTeamAssessment.Team.GetTeamUser(subjectId);
                result.Subject = subject;

                IReadOnlyCollection<IPeerAssessment> allPeerAssessments = projectTeamAssessment.PeerAssessments;
                result.SelfAssessment = allPeerAssessments.FirstOrDefault(pa => pa.IsSelfAssessment && pa.Subject.Id == subject.Id);
                Contracts.Require(result.SelfAssessment != null, "Cannot create an assessment result when the subject has not evaluated itself.");

                if (includePeerAssessments)
                {
                    result.PeerAssessments = allPeerAssessments.Where(pa => pa.Subject.Id == subject.Id && pa.User.Id != subject.Id).ToList();
                    Contracts.Require(result.PeerAssessments.Any(), "Cannot create an assessment result when there is no other peer that evaluated the subject.");
                }
                
                result.AverageResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => (pa.ContributionScore + pa.CooperationScore + pa.EffortScore) / 3.0);
                result.EffortResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.EffortScore);
                result.ContributionResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.ContributionScore);
                result.CooperationResult = _subResultFactory.Create(subject.Id, allPeerAssessments, pa => pa.CooperationScore);

                return result;
            }
        }

    }
}
