using System;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamTests
{
    [Test]
    public void UpdateMetadata_ValidInputs_UpdatesPropertiesAndTimestamp()
    {
        var exam = new ExamBuilder().Build();
        var updatedAt = DateTime.UtcNow.AddMinutes(5);
        var updated = new ExamMetadata("  Final Exam  ", "Updated instructions", "NEW-2026", ExamTemplateCatalog.DotNet10AspNetCoreWebApi);

        exam.UpdateMetadata(updated, updatedAt);

        Assert.That(exam.Title, Is.EqualTo("Final Exam"));
        Assert.That(exam.Description, Is.EqualTo("Updated instructions"));
        Assert.That(exam.AccessCode.Value, Is.EqualTo("NEW-2026"));
        Assert.That(exam.TemplateKey, Is.EqualTo(ExamTemplateCatalog.DotNet10AspNetCoreWebApi));
        Assert.That(exam.UpdatedAtUtc, Is.EqualTo(updatedAt));
    }

    [Test]
    public void UpdateMetadata_InvalidInputs_ThrowsContractException()
    {
        var exam = new ExamBuilder().Build();
        var metadata = new ExamMetadata("Title", "desc", "CODE1", ExamTemplateCatalog.DotNet10Console);

        Assert.Throws<ContractException>(() => exam.UpdateMetadata(null, DateTime.UtcNow));
        Assert.Throws<ContractException>(() => exam.UpdateMetadata(metadata, DateTime.Now));
    }

    [Test]
    public void Open_WithValidQuestionsAndEditableFiles_TransitionsDraftAndClosedToOpen()
    {
        var exam = new ExamBuilder().WithQuestion().Build();
        var openTime = DateTime.UtcNow.AddMinutes(1);

        exam.Open(openTime);
        Assert.That(exam.Status, Is.EqualTo(ExamStatus.Open));
        Assert.That(exam.UpdatedAtUtc, Is.EqualTo(openTime));

        var closeTime = DateTime.UtcNow.AddMinutes(2);
        exam.Close(closeTime);
        Assert.That(exam.Status, Is.EqualTo(ExamStatus.Closed));
        Assert.That(exam.UpdatedAtUtc, Is.EqualTo(closeTime));

        var reopenTime = DateTime.UtcNow.AddMinutes(3);
        exam.Open(reopenTime);
        Assert.That(exam.Status, Is.EqualTo(ExamStatus.Open));
    }

    [Test]
    public void Open_WithoutQuestionsOrWithoutEditableFile_ThrowsContractException()
    {
        var emptyExam = new ExamBuilder().Build();
        Assert.Throws<ContractException>(() => emptyExam.Open(DateTime.UtcNow));

        var readOnlyOnlyExam = new ExamBuilder().WithQuestion(withEditableFile: false).Build();
        var question = System.Linq.Enumerable.First(readOnlyOnlyExam.Questions);
        question.UpsertFile(new ExamFilePath("Program.cs"), "class Program {}", ExamFileRole.ReadOnly);

        Assert.Throws<ContractException>(() => readOnlyOnlyExam.Open(DateTime.UtcNow));
    }

    [Test]
    public void Open_WhenAlreadyOpen_OrCloseWhenNotOpen_ThrowsContractException()
    {
        var exam = new ExamBuilder().WithQuestion().Build();
        Assert.Throws<ContractException>(() => exam.Close(DateTime.UtcNow));

        exam.Open(DateTime.UtcNow);
        Assert.Throws<ContractException>(() => exam.Open(DateTime.UtcNow));
        Assert.Throws<ContractException>(() => exam.Close(DateTime.Now));
    }

    [Test]
    public void EnsureCanEnter_WhenOpenAndCodeMatches_Succeeds_OtherwiseThrowsContractException()
    {
        var exam = new ExamBuilder().WithQuestion().Build();
        Assert.Throws<ContractException>(() => exam.EnsureCanEnter("EXAM-2026"));

        exam.Open(DateTime.UtcNow);
        Assert.DoesNotThrow(() => exam.EnsureCanEnter("EXAM-2026"));
        Assert.Throws<ContractException>(() => exam.EnsureCanEnter("WRONG"));
        Assert.Throws<ContractException>(() => exam.EnsureCanEnter(" "));

        exam.Close(DateTime.UtcNow);
        Assert.Throws<ContractException>(() => exam.EnsureCanEnter("EXAM-2026"));
        Assert.Throws<ContractException>(() => exam.EnsureWorkspaceAccessible(allowUnopenedPreview: false));
        Assert.DoesNotThrow(() => exam.EnsureWorkspaceAccessible(allowUnopenedPreview: true));
    }

    [Test]
    public void AddQuestion_UpdateQuestion_And_RemoveQuestion_WorkAndEnforceInvariants()
    {
        var exam = new ExamBuilder().WithQuestion(orderIndex: 1).Build();

        Assert.Throws<ContractException>(() =>
            exam.AddQuestion("Question 2", "Instructions", orderIndex: 1, ExamTemplateCatalog.DotNet10Console));

        var secondQuestion = exam.AddQuestion("Question 2", "Instructions", orderIndex: 2, ExamTemplateCatalog.DotNet10Console);
        Assert.That(exam.Questions, Has.Count.EqualTo(2));

        Assert.Throws<ContractException>(() => exam.GetQuestion(999));

        var now = DateTime.UtcNow;
        exam.UpdateQuestion(0, "Renamed Q1", "New", 3, ExamTemplateCatalog.DotNet10ClassLibXUnit, now);
        Assert.That(exam.Questions, Has.Some.Matches<ExamQuestion>(q => q.Title == "Renamed Q1" && q.OrderIndex == 3));

        exam.RemoveQuestion(0, now);
        Assert.That(exam.Questions, Has.Count.EqualTo(1));
        Assert.That(secondQuestion.OrderIndex, Is.EqualTo(2));
    }
}
