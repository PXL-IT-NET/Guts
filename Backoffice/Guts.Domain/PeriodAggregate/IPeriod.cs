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
        bool IsActive { get; }

        void Update(string description, DateTime from, DateTime until, IReadOnlyList<IPeriod> allPeriods);

        bool OverlapsWith(DateTime from, DateTime until);
    }
}