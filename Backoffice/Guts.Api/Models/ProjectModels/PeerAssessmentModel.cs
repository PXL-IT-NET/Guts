using System.Text.Json.Serialization;

namespace Guts.Api.Models.ProjectModels;

public class PeerAssessmentModel
{
    public UserModel Subject { get; set; }
    public UserModel User { get; set; }

    [JsonRequired]
    public int ContributionScore { get; set; }

    [JsonRequired]
    public int CooperationScore { get; set; }

    [JsonRequired]
    public int EffortScore { get; set; }

    [JsonRequired]
    public bool IsSelfAssessment { get; set; }

    public string Explanation { get; set; }
}