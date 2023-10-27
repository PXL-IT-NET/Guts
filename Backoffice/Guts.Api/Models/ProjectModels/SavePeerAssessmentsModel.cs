namespace Guts.Api.Models.ProjectModels;

public class SavePeerAssessmentsModel
{
    public PeerAssessmentModel[] PeerAssessments { get; set; }
    public bool IsDraft { get; set; }
}