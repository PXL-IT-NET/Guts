﻿using System;
using Guts.Common.Extensions;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;

namespace Guts.Domain.Tests.Builders
{
    internal class ProjectAssessmentMockBuilder : BaseBuilder<Mock<IProjectAssessment>>
    {
        public ProjectAssessmentMockBuilder()
        {
            Item = new Mock<IProjectAssessment>();
            Item.SetupGet(pa => pa.ProjectId).Returns(Random.Shared.NextPositive());
            Item.SetupGet(pa => pa.Description).Returns(Random.Shared.NextString());
            DateTime openOnUtc = Random.Shared.NextDateTimeInFuture().ToUniversalTime();
            Item.SetupGet(pa => pa.OpenOnUtc).Returns(openOnUtc);
            Item.SetupGet(pa => pa.DeadlineUtc).Returns(openOnUtc.AddDays(15));
        }

        public ProjectAssessmentMockBuilder WithId()
        {
            Item.SetupGet(pa => pa.Id).Returns(Random.Shared.NextPositive());
            return this;
        }

        public ProjectAssessmentMockBuilder WithOpenOn(DateTime openOnUtc)
        {
            Item.SetupGet(pa => pa.OpenOnUtc).Returns(openOnUtc);
            Item.SetupGet(pa => pa.DeadlineUtc).Returns(openOnUtc.AddDays(15));
            return this;
        }
    }
}