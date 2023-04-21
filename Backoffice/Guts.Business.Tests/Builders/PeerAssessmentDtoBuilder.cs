using Guts.Business.Dtos;
using Guts.Common.Extensions;
using Guts.Domain.Tests.Builders;

namespace Guts.Business.Tests.Builders;

internal class PeerAssessmentDtoBuilder : BaseBuilder<PeerAssessmentDto>
{
    public PeerAssessmentDtoBuilder()
    {
        Item = new PeerAssessmentDto
        {
            SubjectId = Random.NextPositive(),
            UserId = Random.NextPositive(),
            ContributionScore = Random.Next(0,6),
            CooperationScore = Random.Next(0, 6),
            EffortScore = Random.Next(0, 6),
            Explanation = Random.NextString()
        };
    }

    public PeerAssessmentDtoBuilder WithUserId(int userId)
    {
        Item.UserId = userId;
        return this;
    }
}