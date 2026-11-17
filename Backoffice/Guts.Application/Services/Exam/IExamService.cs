using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos.Exam;

namespace Guts.Application.Services.Exam;

public interface IExamService
{
    Task<IReadOnlyList<ExamSummaryDto>> GetCourseExamsAsync(int courseId, int? periodId = null);
    Task<ExamDetailDto> GetExamForLectorAsync(int examId);
    Task<ExamDetailDto> CreateExamAsync(CreateExamRequestDto request);
    Task<ExamDetailDto> UpdateExamAsync(int examId, UpdateExamRequestDto request);
    Task<ExamDetailDto> OpenExamAsync(int examId);
    Task<ExamDetailDto> CloseExamAsync(int examId);
    Task DeleteExamAsync(int examId);
    Task<ExamDetailDto> AddQuestionAsync(int examId, SaveExamQuestionDto request);
    Task<ExamDetailDto> UpdateQuestionAsync(int examId, int questionId, SaveExamQuestionDto request);
    Task<ExamDetailDto> DeleteQuestionAsync(int examId, int questionId);
    Task<ExamDetailDto> ImportQuestionFilesFromZipAsync(int examId, int questionId, Stream zipStream, CancellationToken cancellationToken = default);
    Task<ExamDetailDto> ImportQuestionFilesFromGitAsync(int examId, int questionId, string repositoryUrl, string branchOrRef = null, CancellationToken cancellationToken = default);
    Task<ExamDetailDto> UpdateQuestionFilesAsync(int examId, int questionId, IReadOnlyList<FileRoleAssignmentDto> updates);
    Task<ExamDetailDto> DeleteQuestionFileAsync(int examId, int questionId, int fileId);
    Task<ExamEntryResultDto> EnterExamAsync(int examId, string accessCode);
    Task<ExamWorkspaceDto> GetExamWorkspaceAsync(int examId, bool allowUnopenedPreview = false);
}
