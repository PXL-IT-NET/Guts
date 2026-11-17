using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamQuestion : Entity
{
    public const int MaxTitleLength = 200;

    private readonly List<ExamQuestionFile> _files = [];

    public int Id { get; private set; }
    public int ExamId { get; private set; }
    public string Title { get; private set; }
    public string InstructionsMarkdown { get; private set; }
    public int OrderIndex { get; private set; }
    public string TemplateKey { get; private set; }

    public virtual IReadOnlyCollection<ExamQuestionFile> Files => _files.AsReadOnly();

    private ExamQuestion()
    {
    }

    internal ExamQuestion(
        int examId,
        string title,
        string instructionsMarkdown,
        int orderIndex,
        string templateKey,
        int id = 0)
    {
        ValidateDetails(title, orderIndex, templateKey);

        Id = id;
        ExamId = examId;
        Title = title.Trim();
        InstructionsMarkdown = instructionsMarkdown ?? string.Empty;
        OrderIndex = orderIndex;
        TemplateKey = ExamTemplateCatalog.GetByKey(templateKey).Key;
    }

    public void UpdateDetails(string title, string instructionsMarkdown, int orderIndex, string templateKey)
    {
        ValidateDetails(title, orderIndex, templateKey);

        Title = title.Trim();
        InstructionsMarkdown = instructionsMarkdown ?? string.Empty;
        OrderIndex = orderIndex;
        TemplateKey = ExamTemplateCatalog.GetByKey(templateKey).Key;
    }

    public ExamQuestionFile UpsertFile(ExamFilePath path, string content, ExamFileRole role)
    {
        Contracts.Require(path != null, "File path cannot be null.");

        var existing = _files.FirstOrDefault(f =>
            string.Equals(f.RelativePath.Value, path.Value, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.UpdatePathAndContent(path, content, role);
            return existing;
        }

        var file = new ExamQuestionFile(Id, path, content, role);
        _files.Add(file);
        return file;
    }

    public void SetFileRole(int fileId, ExamFileRole role)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId);
        Contracts.Require(file != null, $"Exam file {fileId} was not found in question '{Title}'.");
        file.ChangeRole(role);
    }

    public void ReplaceFiles(IEnumerable<(ExamFilePath Path, string Content, ExamFileRole Role)> files)
    {
        Contracts.Require(files != null, "Files collection cannot be null.");

        var fileList = files.ToList();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in fileList.Select(item => item.Path))
        {
            Contracts.Require(path != null, "File path cannot be null.");
            Contracts.Require(
                seenPaths.Add(path.Value),
                $"Duplicate file path '{path.Value}' in question '{Title}'.");
        }

        _files.Clear();
        foreach (var item in fileList)
        {
            _files.Add(new ExamQuestionFile(Id, item.Path, item.Content, item.Role));
        }
    }

    public void RemoveFile(int fileId)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId);
        Contracts.Require(file != null, $"Exam file {fileId} was not found in question '{Title}'.");
        _files.Remove(file);
    }

    public void EnsureValidForOpen()
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(Title), "Exam question title cannot be empty.");
        Contracts.Require(ExamTemplateCatalog.IsSupported(TemplateKey), $"Unsupported template '{TemplateKey}' on question '{Title}'.");
        Contracts.Require(
            _files.Any(f => f.Role == ExamFileRole.Editable),
            $"Exam question '{Title}' must contain at least one editable file before opening.");
    }

    private static void ValidateDetails(string title, int orderIndex, string templateKey)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(title), "Exam question title cannot be empty.");
        Contracts.Require(title.Trim().Length <= MaxTitleLength, $"Exam question title cannot exceed {MaxTitleLength} characters.");
        Contracts.Require(orderIndex >= 1, "Question order index must be at least 1.");
        Contracts.Require(ExamTemplateCatalog.IsSupported(templateKey), $"Unsupported template key '{templateKey}'.");
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}
