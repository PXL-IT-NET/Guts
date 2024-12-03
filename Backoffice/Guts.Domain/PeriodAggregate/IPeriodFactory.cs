using System;
using System.Collections;
using System.Collections.Generic;

namespace Guts.Domain.PeriodAggregate
{
    public interface IPeriodFactory
    {
        Period CreateNew(string description, DateTime from, DateTime until, IList<IPeriod> existingPeriods);
    }
}