using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class ExamQuestionFile : Entity
{
    public int Id { get; private set; }
    public int ExamQuestionId { get; private set; }
    public ExamFilePath RelativePath { get; private set; }
    public string Content { get; private set; }
    public ExamFileRole Role { get; private set; }

    private ExamQuestionFile()
    {
    }

    internal ExamQuestionFile(
        int examQuestionId,
        ExamFilePath relativePath,
        string content,
        ExamFileRole role,
        int id = 0)
    {
        Contracts.Require(relativePath != null, "Relative path cannot be null.");
        Contracts.Require(content != null, "File content cannot be null.");
        Contracts.Require(Enum.IsDefined(role), "Invalid exam file role.");

        Id = id;
        ExamQuestionId = examQuestionId;
        RelativePath = relativePath;
        Content = content;
        Role = role;
    }

    public void ChangeRole(ExamFileRole role)
    {
        Contracts.Require(Enum.IsDefined(role), "Invalid exam file role.");
        Role = role;
    }

    public void UpdatePathAndContent(ExamFilePath relativePath, string content, ExamFileRole role)
    {
        Contracts.Require(relativePath != null, "Relative path cannot be null.");
        Contracts.Require(content != null, "File content cannot be null.");
        Contracts.Require(Enum.IsDefined(role), "Invalid exam file role.");

        RelativePath = relativePath;
        Content = content;
        Role = role;
    }

    public void UpdateContent(string content)
    {
        Contracts.Require(content != null, "File content cannot be null.");
        Content = content;
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}
