using System.Collections.Generic;
using System.Dynamic;

namespace Guts.Domain.ExamAggregate
{
    public interface IExamScore
    {
        string FirstName { get; }
        string LastName { get; }
        double Score { get; }
        double NormalizedScore { get; }
        double MaximumScore { get; }
        double NormalizedMaximumScore { get; }
        IReadOnlyList<IExamPartScore> ExamPartScores { get; }
        ExpandoObject ToCsvRecord();
    }
}