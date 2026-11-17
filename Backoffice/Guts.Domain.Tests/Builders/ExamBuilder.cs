using System;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;

namespace Guts.Domain.Tests.Builders;

internal class ExamBuilder : BaseBuilder<Exam>
{
    public ExamBuilder()
    {
        var metadata = new ExamMetadata(
            title: "OOP Midterm Exam",
            description: "# Instructions\nImplement the required classes.",
            accessCode: "EXAM-2026",
            templateKey: ExamTemplateCatalog.DotNet10ClassLibNUnit);

        Item = new ExamFactory().CreateNew(
            courseId: Random.Shared.NextPositive(),
            periodId: Random.Shared.NextPositive(),
            createdByUserId: Random.Shared.NextPositive(),
            metadata: metadata,
            createdAtUtc: DateTime.UtcNow);
    }

    public ExamBuilder WithId(int id)
    {
        SetProperty(e => e.Id, id);
        return this;
    }

    public ExamBuilder WithStatus(ExamStatus status)
    {
        SetProperty(e => e.Status, status);
        return this;
    }

    public ExamBuilder WithQuestion(
        string title = "Question 1",
        string instructions = "Implement `Calculator.Add`.",
        int orderIndex = 1,
        string templateKey = ExamTemplateCatalog.DotNet10ClassLibNUnit,
        bool withEditableFile = true)
    {
        var question = Item.AddQuestion(title, instructions, orderIndex, templateKey);
        if (withEditableFile)
        {
            question.UpsertFile(new ExamFilePath("Calculator.cs"), "public class Calculator {}", ExamFileRole.Editable);
        }

        return this;
    }
}
