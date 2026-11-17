using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Application.Dtos.Exam;
using Guts.Application.Services.Exam;
using Guts.Domain.ExamAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/exams")]
[Authorize(Policy = ApiConstants.LectorOrAdminPolicy)]
public class ExamController(
    IExamService examService,
    IMapper mapper)
    : ControllerBase
{
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IList<ExamTemplateOptionModel>), (int)HttpStatusCode.OK)]
    public IActionResult GetTemplates()
    {
        var templates = ExamTemplateCatalog.All
            .Select(t => new ExamTemplateOptionModel
            {
                Key = t.Key,
                DisplayName = t.DisplayName,
                SupportsWasmExecution = t.SupportsWasmExecution
            })
            .ToList();

        return Ok(templates);
    }

    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<ExamSummaryModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetCourseExams(int courseId, [FromQuery] int? periodId = null)
    {
        if (courseId < 1) return BadRequest();

        var exams = await examService.GetCourseExamsAsync(courseId, periodId);
        return Ok(exams.Select(mapper.MapToExamSummaryModel).ToList());
    }

    [HttpGet("{examId}")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetExam(int examId)
    {
        if (examId < 1) return BadRequest();

        var detail = await examService.GetExamForLectorAsync(examId);
        return Ok(mapper.MapToExamDetailModel(detail));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var created = await examService.CreateExamAsync(new CreateExamRequestDto
        {
            CourseId = model.CourseId,
            PeriodId = model.PeriodId,
            CreatedByUserId = GetUserId(),
            Title = model.Title,
            Description = model.Description,
            AccessCode = model.AccessCode,
            TemplateKey = model.TemplateKey
        });

        var outputModel = mapper.MapToExamDetailModel(created);
        return CreatedAtAction(nameof(GetExam), new { examId = created.Id }, outputModel);
    }

    [HttpPut("{examId}")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateExam(int examId, [FromBody] UpdateExamModel model)
    {
        if (examId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await examService.UpdateExamAsync(examId, new UpdateExamRequestDto
        {
            Title = model.Title,
            Description = model.Description,
            AccessCode = model.AccessCode,
            TemplateKey = model.TemplateKey
        });

        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPost("{examId}/open")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> OpenExam(int examId)
    {
        if (examId < 1) return BadRequest();

        var updated = await examService.OpenExamAsync(examId);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPost("{examId}/close")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CloseExam(int examId)
    {
        if (examId < 1) return BadRequest();

        var updated = await examService.CloseExamAsync(examId);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpDelete("{examId}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> DeleteExam(int examId)
    {
        if (examId < 1) return BadRequest();

        await examService.DeleteExamAsync(examId);
        return NoContent();
    }

    [HttpPost("{examId}/questions")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddQuestion(int examId, [FromBody] SaveExamQuestionModel model)
    {
        if (examId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await examService.AddQuestionAsync(examId, new SaveExamQuestionDto
        {
            Title = model.Title,
            InstructionsMarkdown = model.InstructionsMarkdown,
            OrderIndex = model.OrderIndex,
            TemplateKey = model.TemplateKey
        });

        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPut("{examId}/questions/{questionId}")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdateQuestion(int examId, int questionId, [FromBody] SaveExamQuestionModel model)
    {
        if (examId < 1 || questionId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await examService.UpdateQuestionAsync(examId, questionId, new SaveExamQuestionDto
        {
            Title = model.Title,
            InstructionsMarkdown = model.InstructionsMarkdown,
            OrderIndex = model.OrderIndex,
            TemplateKey = model.TemplateKey
        });

        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpDelete("{examId}/questions/{questionId}")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> DeleteQuestion(int examId, int questionId)
    {
        if (examId < 1 || questionId < 1) return BadRequest();

        var updated = await examService.DeleteQuestionAsync(examId, questionId);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPost("{examId}/questions/{questionId}/import-zip")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ImportFromZip(
        int examId,
        int questionId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (examId < 1 || questionId < 1) return BadRequest();
        if (file == null || file.Length <= 0)
        {
            return BadRequest(ErrorModel.FromString("A non-empty zip archive file is required."));
        }

        await using var stream = file.OpenReadStream();
        var updated = await examService.ImportQuestionFilesFromZipAsync(examId, questionId, stream, cancellationToken);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPost("{examId}/questions/{questionId}/import-git")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ImportFromGit(
        int examId,
        int questionId,
        [FromBody] ImportGitModel model,
        CancellationToken cancellationToken)
    {
        if (examId < 1 || questionId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await examService.ImportQuestionFilesFromGitAsync(
            examId,
            questionId,
            model.RepositoryUrl,
            model.BranchOrRef,
            cancellationToken);

        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpPut("{examId}/questions/{questionId}/files")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> UpdateFiles(int examId, int questionId, [FromBody] UpdateExamFilesModel model)
    {
        if (examId < 1 || questionId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updates = model.Files
            .Select(f => new FileRoleAssignmentDto
            {
                FileId = f.FileId,
                RelativePath = f.RelativePath,
                Content = f.Content,
                Role = f.Role
            })
            .ToList();

        var updated = await examService.UpdateQuestionFilesAsync(examId, questionId, updates);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpDelete("{examId}/questions/{questionId}/files/{fileId}")]
    [ProducesResponseType(typeof(ExamDetailModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> DeleteFile(int examId, int questionId, int fileId)
    {
        if (examId < 1 || questionId < 1 || fileId < 1) return BadRequest();

        var updated = await examService.DeleteQuestionFileAsync(examId, questionId, fileId);
        return Ok(mapper.MapToExamDetailModel(updated));
    }

    [HttpGet("{examId}/preview")]
    [ProducesResponseType(typeof(ExamWorkspaceModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PreviewExam(int examId)
    {
        if (examId < 1) return BadRequest();

        var workspace = await examService.GetExamWorkspaceAsync(examId, allowUnopenedPreview: true);
        return Ok(mapper.MapToExamWorkspaceModel(workspace));
    }
}
