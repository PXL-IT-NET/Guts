using System;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;

namespace Guts.Domain.Tests.Builders;

internal class PeriodBuilder : BaseBuilder<Period>
{
    public PeriodBuilder()
    {
        DateTime from = Random.Shared.NextDateTimeInPast();
        DateTime until = Random.Shared.NextDateTimeInFuture();
        string description = Random.Shared.NextString();

        ConstructItem(description,from, until);
    }

    public PeriodBuilder WithId()
    {
        SetProperty(p => p.Id, Random.Shared.NextPositive());
        return this;
    }
}