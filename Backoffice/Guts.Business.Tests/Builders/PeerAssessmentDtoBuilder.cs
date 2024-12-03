using System;
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
            SubjectId = Random.Shared.NextPositive(),
            UserId = Random.Shared.NextPositive(),
            ContributionScore = Random.Shared.Next(0,6),
            CooperationScore = Random.Shared.Next(0, 6),
            EffortScore = Random.Shared.Next(0, 6),
            Explanation = Random.Shared.NextString()
        };
    }

    public PeerAssessmentDtoBuilder WithUserId(int userId)
    {
        Item.UserId = userId;
        return this;
    }
}