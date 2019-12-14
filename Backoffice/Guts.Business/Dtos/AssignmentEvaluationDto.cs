namespace Guts.Business.Dtos
{
    public class AssignmentEvaluationDto
    {
        public int AssignmentId { get; set; }
        public int MaximumScore { get; set; }
        public int NumberOfTestsAlreadyGreenAtStart { get; set; }
    }
}