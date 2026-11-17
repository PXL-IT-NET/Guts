using Guts.Application.Dtos.Exam;
using ExamEntity = Guts.Domain.ExamAggregate.Exam;

namespace Guts.Application.Converters;

public interface IExamConverter
{
    ExamSummaryDto ToSummaryDto(ExamEntity exam);
    ExamDetailDto ToDetailDto(ExamEntity exam);
    ExamWorkspaceDto ToWorkspaceDto(ExamEntity exam);
}
