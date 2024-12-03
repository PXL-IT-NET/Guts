using System;
using System.Collections;
using System.Collections.Generic;

namespace Guts.Domain.PeriodAggregate
{
    public interface IPeriod : IEntity
    {
        string Description { get; set; }
        DateTime From { get; }
        DateTime Until { get; }

        void Update(string description, DateTime from, DateTime until, IList<IPeriod> allPeriods);
    }
}