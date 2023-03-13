using System.Collections.Generic;

namespace Guts.Business.Dtos
{
    public class ProjectTeamAssessmentStatusDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public bool IsComplete { get; set; }
        public IReadOnlyList<PeerDto> PeersThatNeedToEvaluateOthers { get; set; }
    }
}