using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Converters;
using Guts.Application.Dtos.Exam;
using Guts.Application.Repositories;
using Guts.Application.Security;
using Guts.Domain.ExamAggregate;

namespace Guts.Application.Services.Exam;

internal class ExamService(
    IExamRepository examRepository,
    IExamFactory examFactory,
    IPeriodRepository periodRepository,
    IExamProjectImporter projectImporter,
    IExamConverter examConverter,
    ITokenAccessPassFactory tokenAccessPassFactory)
    : IExamService
{
    public async Task<IReadOnlyList<ExamSummaryDto>> GetCourseExamsAsync(int courseId, int? periodId = null)
    {
        var period = await periodRepository.GetPeriodAsync(periodId);
        var exams = await examRepository.GetByCourseAndPeriodAsync(courseId, period.Id);
        return exams.Select(examConverter.ToSummaryDto).ToList();
    }

    public async Task<ExamDetailDto> GetExamForLectorAsync(int examId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> CreateExamAsync(CreateExamRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var period = await periodRepository.GetPeriodAsync(request.PeriodId);
        var metadata = new ExamMetadata(request.Title, request.Description, request.AccessCode, request.TemplateKey);
        var exam = examFactory.CreateNew(
            request.CourseId,
            period.Id,
            request.CreatedByUserId,
            metadata,
            DateTime.UtcNow);

        var persisted = await examRepository.AddAsync(exam);
        return examConverter.ToDetailDto(persisted);
    }

    public async Task<ExamDetailDto> UpdateExamAsync(int examId, UpdateExamRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        var metadata = new ExamMetadata(request.Title, request.Description, request.AccessCode, request.TemplateKey);
        exam.UpdateMetadata(metadata, DateTime.UtcNow);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> OpenExamAsync(int examId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.Open(DateTime.UtcNow);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> CloseExamAsync(int examId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.Close(DateTime.UtcNow);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task DeleteExamAsync(int examId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        await examRepository.DeleteAsync(exam);
    }

    public async Task<ExamDetailDto> AddQuestionAsync(int examId, SaveExamQuestionDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.AddQuestion(request.Title, request.InstructionsMarkdown, request.OrderIndex, request.TemplateKey);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> UpdateQuestionAsync(int examId, int questionId, SaveExamQuestionDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.UpdateQuestion(
            questionId,
            request.Title,
            request.InstructionsMarkdown,
            request.OrderIndex,
            request.TemplateKey,
            DateTime.UtcNow);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> DeleteQuestionAsync(int examId, int questionId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.RemoveQuestion(questionId, DateTime.UtcNow);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> ImportQuestionFilesFromZipAsync(
        int examId,
        int questionId,
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        var question = exam.GetQuestion(questionId);
        var importedFiles = await projectImporter.ExtractFromZipAsync(zipStream, cancellationToken);
        question.ReplaceFiles(importedFiles.Select(f => (new ExamFilePath(f.RelativePath), f.Content, f.SuggestedRole)));
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> ImportQuestionFilesFromGitAsync(
        int examId,
        int questionId,
        string repositoryUrl,
        string branchOrRef = null,
        CancellationToken cancellationToken = default)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        var question = exam.GetQuestion(questionId);
        var importedFiles = await projectImporter.CloneFromGitAsync(repositoryUrl, branchOrRef, cancellationToken);
        question.ReplaceFiles(importedFiles.Select(f => (new ExamFilePath(f.RelativePath), f.Content, f.SuggestedRole)));
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> UpdateQuestionFilesAsync(
        int examId,
        int questionId,
        IReadOnlyList<FileRoleAssignmentDto> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);

        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        var question = exam.GetQuestion(questionId);
        foreach (var update in updates)
        {
            if (update.Content == null && update.FileId.HasValue)
            {
                question.SetFileRole(update.FileId.Value, update.Role);
            }
            else
            {
                question.UpsertFile(new ExamFilePath(update.RelativePath), update.Content, update.Role);
            }
        }

        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamDetailDto> DeleteQuestionFileAsync(int examId, int questionId, int fileId)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        var question = exam.GetQuestion(questionId);
        question.RemoveFile(fileId);
        await examRepository.UpdateAsync(exam);
        return examConverter.ToDetailDto(exam);
    }

    public async Task<ExamEntryResultDto> EnterExamAsync(int examId, string accessCode)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.EnsureCanEnter(accessCode);

        var pass = tokenAccessPassFactory.CreateExamGuestToken(exam.Id);
        var workspace = examConverter.ToWorkspaceDto(exam);
        return new ExamEntryResultDto
        {
            AccessPass = pass,
            Workspace = workspace
        };
    }

    public async Task<ExamWorkspaceDto> GetExamWorkspaceAsync(int examId, bool allowUnopenedPreview = false)
    {
        var exam = await examRepository.GetByIdWithQuestionsAsync(examId);
        exam.EnsureWorkspaceAccessible(allowUnopenedPreview);
        return examConverter.ToWorkspaceDto(exam);
    }
}
