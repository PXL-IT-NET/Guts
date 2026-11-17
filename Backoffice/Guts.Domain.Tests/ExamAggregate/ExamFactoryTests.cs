using System;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamFactoryTests
{
    private ExamFactory _factory;

    [SetUp]
    public void Setup()
    {
        _factory = new ExamFactory();
    }

    [Test]
    public void CreateNew_ValidInputs_CreatesDraftExam()
    {
        var now = DateTime.UtcNow;
        var metadata = new ExamMetadata(
            "  .NET Exam  ",
            "Read carefully.",
            "PXL-99",
            ExamTemplateCatalog.DotNet10Console);

        var exam = _factory.CreateNew(
            courseId: 10,
            periodId: 2,
            createdByUserId: 99,
            metadata: metadata,
            createdAtUtc: now);

        Assert.That(exam.CourseId, Is.EqualTo(10));
        Assert.That(exam.PeriodId, Is.EqualTo(2));
        Assert.That(exam.CreatedByUserId, Is.EqualTo(99));
        Assert.That(exam.Title, Is.EqualTo(".NET Exam"));
        Assert.That(exam.Description, Is.EqualTo("Read carefully."));
        Assert.That(exam.AccessCode.Value, Is.EqualTo("PXL-99"));
        Assert.That(exam.TemplateKey, Is.EqualTo(ExamTemplateCatalog.DotNet10Console));
        Assert.That(exam.Status, Is.EqualTo(ExamStatus.Draft));
        Assert.That(exam.CreatedAtUtc, Is.EqualTo(now));
        Assert.That(exam.UpdatedAtUtc, Is.EqualTo(now));
        Assert.That(exam.Questions, Is.Empty);
    }

    [Test]
    public void CreateNew_InvalidInputs_ThrowsContractException()
    {
        var utcNow = DateTime.UtcNow;
        var metadata = new ExamMetadata("Title", "", "CODE1", ExamTemplateCatalog.DotNet10Console);

        Assert.Throws<ContractException>(() => _factory.CreateNew(0, 1, 1, metadata, utcNow));
        Assert.Throws<ContractException>(() => _factory.CreateNew(1, 0, 1, metadata, utcNow));
        Assert.Throws<ContractException>(() => _factory.CreateNew(1, 1, 0, metadata, utcNow));
        Assert.Throws<ContractException>(() => _factory.CreateNew(1, 1, 1, null, utcNow));
        Assert.Throws<ContractException>(() => _factory.CreateNew(1, 1, 1, metadata, DateTime.Now));
        Assert.Throws<ContractException>(() => _ = new ExamMetadata(" ", "", "CODE1", ExamTemplateCatalog.DotNet10Console));
        Assert.Throws<ContractException>(() => _ = new ExamMetadata(new string('A', ExamMetadata.MaxTitleLength + 1), "", "CODE1", ExamTemplateCatalog.DotNet10Console));
        Assert.Throws<ContractException>(() => _ = new ExamMetadata("Title", "", "ab", ExamTemplateCatalog.DotNet10Console));
        Assert.Throws<ContractException>(() => _ = new ExamMetadata("Title", "", "CODE1", "invalid-template"));
    }
}
