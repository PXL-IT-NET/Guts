using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models.Converters
{
    public interface IEvaluationConverter
    {
        ExamPartOutputModel ToEvaluationOutputModel(ExamPart examPart);
    }
}