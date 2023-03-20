using Guts.Domain.ValueObjects;

namespace Guts.Business.Dtos
{
    public class StudentAssessmentResultDto
    {
        public double Average { get; set; }
        public double TeamAverage { get; set; }
        public double SelfAverage { get; set; }
        public double PeerAverage { get; set; }

        public AssessmentScore Score { get; set; }
        public AssessmentScore SelfScore { get; set; }
        public AssessmentScore PeerScore { get; set; }
    }
}