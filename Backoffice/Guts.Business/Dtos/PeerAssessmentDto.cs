namespace Guts.Business.Dtos
{
    public class PeerAssessmentDto
    {
        public int SubjectId { get; set; }
        public int UserId { get; set; }

        public int ContributionScore { get; set; }
        public int CooperationScore { get; set; }
        public int EffortScore { get; set; }

        public string Explanation { get; set; }
    }
}