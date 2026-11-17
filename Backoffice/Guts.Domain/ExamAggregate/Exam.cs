using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.ExamAggregate;

public class Exam : AggregateRoot
{
    public const int MaxTitleLength = ExamMetadata.MaxTitleLength;

    private readonly List<ExamQuestion> _questions = [];

    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int CourseId { get; private set; }
    public int PeriodId { get; private set; }
    public ExamAccessCode AccessCode { get; private set; }
    public string TemplateKey { get; private set; }
    public ExamStatus Status { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public virtual IReadOnlyCollection<ExamQuestion> Questions => _questions.AsReadOnly();

    private Exam()
    {
    }

    internal Exam(
        int courseId,
        int periodId,
        int createdByUserId,
        ExamMetadata metadata,
        DateTime createdAtUtc,
        int id = 0)
    {
        Contracts.Require(courseId > 0, "Course identifier must be positive.");
        Contracts.Require(periodId > 0, "Period identifier must be positive.");
        Contracts.Require(createdByUserId > 0, "Creator identifier must be positive.");
        Contracts.Require(metadata != null, "Exam metadata cannot be null.");
        Contracts.Require(createdAtUtc.Kind == DateTimeKind.Utc, "Creation timestamp must be in UTC.");

        Id = id;
        CourseId = courseId;
        PeriodId = periodId;
        CreatedByUserId = createdByUserId;
        ApplyMetadata(metadata);
        Status = ExamStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public void UpdateMetadata(ExamMetadata metadata, DateTime utcNow)
    {
        Contracts.Require(metadata != null, "Exam metadata cannot be null.");
        Contracts.Require(utcNow.Kind == DateTimeKind.Utc, "Timestamp must be in UTC.");

        ApplyMetadata(metadata);
        UpdatedAtUtc = utcNow;
    }

    public ExamQuestion AddQuestion(string title, string instructionsMarkdown, int orderIndex, string templateKey)
    {
        Contracts.Require(
            _questions.All(q => q.OrderIndex != orderIndex),
            $"A question with order index {orderIndex} already exists on this exam.");

        var question = new ExamQuestion(Id, title, instructionsMarkdown, orderIndex, templateKey);
        _questions.Add(question);
        return question;
    }

    public ExamQuestion GetQuestion(int questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        Contracts.Require(question != null, $"Exam question {questionId} was not found on exam '{Title}'.");
        return question;
    }

    public void UpdateQuestion(int questionId, string title, string instructionsMarkdown, int orderIndex, string templateKey, DateTime utcNow)
    {
        Contracts.Require(utcNow.Kind == DateTimeKind.Utc, "Timestamp must be in UTC.");
        var question = GetQuestion(questionId);
        Contracts.Require(
            _questions.All(q => q.Id == questionId || q.OrderIndex != orderIndex),
            $"A question with order index {orderIndex} already exists on this exam.");

        question.UpdateDetails(title, instructionsMarkdown, orderIndex, templateKey);
        UpdatedAtUtc = utcNow;
    }

    public void RemoveQuestion(int questionId, DateTime utcNow)
    {
        Contracts.Require(utcNow.Kind == DateTimeKind.Utc, "Timestamp must be in UTC.");
        var question = GetQuestion(questionId);
        _questions.Remove(question);
        UpdatedAtUtc = utcNow;
    }

    public void Open(DateTime utcNow)
    {
        Contracts.Require(utcNow.Kind == DateTimeKind.Utc, "Timestamp must be in UTC.");
        Contracts.Require(
            Status is ExamStatus.Draft or ExamStatus.Closed,
            "Only a draft or closed exam can be opened.");
        Contracts.Require(AccessCode != null, "An access code is required before opening an exam.");
        Contracts.Require(_questions.Count > 0, "An exam must contain at least one question before opening.");

        foreach (var question in _questions)
        {
            question.EnsureValidForOpen();
        }

        Status = ExamStatus.Open;
        UpdatedAtUtc = utcNow;
    }

    public void Close(DateTime utcNow)
    {
        Contracts.Require(utcNow.Kind == DateTimeKind.Utc, "Timestamp must be in UTC.");
        Contracts.Require(Status == ExamStatus.Open, "Only an open exam can be closed.");

        Status = ExamStatus.Closed;
        UpdatedAtUtc = utcNow;
    }

    public void EnsureCanEnter(string suppliedAccessCode)
    {
        Contracts.Require(Status == ExamStatus.Open, "This exam is not currently open.");
        Contracts.Require(AccessCode.Matches(suppliedAccessCode), "Invalid exam access code.");
    }

    public void EnsureWorkspaceAccessible(bool allowUnopenedPreview)
    {
        Contracts.Require(allowUnopenedPreview || Status == ExamStatus.Open, "This exam is not currently open.");
    }

    private void ApplyMetadata(ExamMetadata metadata)
    {
        Title = metadata.Title;
        Description = metadata.Description;
        AccessCode = metadata.AccessCode;
        TemplateKey = metadata.TemplateKey;
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}
