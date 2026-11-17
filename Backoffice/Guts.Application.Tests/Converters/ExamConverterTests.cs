using System;
using System.Linq;
using Guts.Application.Converters;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Application.Tests.Converters;

[TestFixture]
public class ExamConverterTests
{
    private ExamConverter _converter;

    [SetUp]
    public void Setup()
    {
        _converter = new ExamConverter();
    }

    [Test]
    public void ToSummaryDto_MapsExamFieldsAndQuestionCount()
    {
        var exam = new ExamBuilder().WithId(15).WithQuestion().Build();

        var dto = _converter.ToSummaryDto(exam);

        Assert.That(dto.Id, Is.EqualTo(15));
        Assert.That(dto.Title, Is.EqualTo(exam.Title));
        Assert.That(dto.CourseId, Is.EqualTo(exam.CourseId));
        Assert.That(dto.PeriodId, Is.EqualTo(exam.PeriodId));
        Assert.That(dto.TemplateKey, Is.EqualTo(exam.TemplateKey));
        Assert.That(dto.Status, Is.EqualTo(exam.Status));
        Assert.That(dto.QuestionCount, Is.EqualTo(1));
    }

    [Test]
    public void ToDetailDto_IncludesAccessCodeQuestionsAndOrderedFiles()
    {
        var exam = new ExamBuilder().WithId(7).WithQuestion(orderIndex: 2).Build();
        var q1 = exam.AddQuestion("First", "Md", 1, ExamTemplateCatalog.DotNet10Console);
        q1.UpsertFile(new ExamFilePath("b.cs"), "b", ExamFileRole.ReadOnly);
        q1.UpsertFile(new ExamFilePath("a.cs"), "a", ExamFileRole.Editable);

        var dto = _converter.ToDetailDto(exam);

        Assert.That(dto.AccessCode, Is.EqualTo(exam.AccessCode.Value));
        Assert.That(dto.Questions.Select(q => q.OrderIndex), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(dto.Questions[0].Files.Select(f => f.RelativePath), Is.EqualTo(new[] { "a.cs", "b.cs" }));
    }

    [Test]
    public void ToWorkspaceDto_MapsQuestionsWithoutExposingAccessCode()
    {
        var exam = new ExamBuilder().WithId(8).WithQuestion().Build();

        var dto = _converter.ToWorkspaceDto(exam);

        Assert.That(dto.ExamId, Is.EqualTo(8));
        Assert.That(dto.Title, Is.EqualTo(exam.Title));
        Assert.That(dto.Description, Is.EqualTo(exam.Description));
        Assert.That(dto.Questions, Has.Count.EqualTo(1));
    }

    [Test]
    public void NullExam_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _converter.ToSummaryDto(null));
        Assert.Throws<ArgumentNullException>(() => _converter.ToDetailDto(null));
        Assert.Throws<ArgumentNullException>(() => _converter.ToWorkspaceDto(null));
    }
}
