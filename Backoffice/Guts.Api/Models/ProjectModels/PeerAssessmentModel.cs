
namespace Guts.Api.Models.ProjectModels;

public class PeerAssessmentModel
{
    public UserModel Subject { get; set; }
    public UserModel User { get; set; }

    public int ContributionScore { get; set; }
    public int CooperationScore { get; set; }
    public int EffortScore { get; set; }

    public bool IsSelfAssessment { get; set; }

    public string Explanation { get; set; }
}