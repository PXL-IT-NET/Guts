using System.Linq;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamQuestionTests
{
    [Test]
    public void UpsertFile_CaseInsensitivePath_UpdatesExistingInsteadOfDuplicating()
    {
        var exam = new ExamBuilder().Build();
        var question = exam.AddQuestion("Q1", "Solve it", 1, ExamTemplateCatalog.DotNet10Console);

        question.UpsertFile(new ExamFilePath("src/Program.cs"), "v1", ExamFileRole.Editable);
        question.UpsertFile(new ExamFilePath("SRC/program.cs"), "v2", ExamFileRole.ReadOnly);

        Assert.That(question.Files, Has.Count.EqualTo(1));
        var file = question.Files.Single();
        Assert.That(file.RelativePath.Value, Is.EqualTo("SRC/program.cs"));
        Assert.That(file.Content, Is.EqualTo("v2"));
        Assert.That(file.Role, Is.EqualTo(ExamFileRole.ReadOnly));
    }

    [Test]
    public void ReplaceFiles_DuplicatePathsCaseInsensitive_ThrowsContractException()
    {
        var exam = new ExamBuilder().Build();
        var question = exam.AddQuestion("Q1", "Solve it", 1, ExamTemplateCatalog.DotNet10Console);

        Assert.Throws<ContractException>(() => question.ReplaceFiles(
        [
            (new ExamFilePath("Program.cs"), "a", ExamFileRole.Editable),
            (new ExamFilePath("program.cs"), "b", ExamFileRole.ReadOnly)
        ]));
    }

    [Test]
    public void UpdateDetails_ValidAndInvalidInputs_EnforcesInvariants()
    {
        var exam = new ExamBuilder().Build();
        var question = exam.AddQuestion("Q1", "Solve it", 1, ExamTemplateCatalog.DotNet10Console);

        question.UpdateDetails("  Updated Q1 ", "New markdown", 2, ExamTemplateCatalog.DotNet10ClassLibXUnit);

        Assert.That(question.Title, Is.EqualTo("Updated Q1"));
        Assert.That(question.InstructionsMarkdown, Is.EqualTo("New markdown"));
        Assert.That(question.OrderIndex, Is.EqualTo(2));
        Assert.That(question.TemplateKey, Is.EqualTo(ExamTemplateCatalog.DotNet10ClassLibXUnit));

        Assert.Throws<ContractException>(() => question.UpdateDetails(" ", "md", 1, ExamTemplateCatalog.DotNet10Console));
        Assert.Throws<ContractException>(() => question.UpdateDetails("Q1", "md", 0, ExamTemplateCatalog.DotNet10Console));
        Assert.Throws<ContractException>(() => question.UpdateDetails("Q1", "md", 1, "bad-template"));
    }

    [Test]
    public void SetFileRole_And_RemoveFile_WhenMissing_ThrowsContractException()
    {
        var exam = new ExamBuilder().Build();
        var question = exam.AddQuestion("Q1", "Solve it", 1, ExamTemplateCatalog.DotNet10Console);

        Assert.Throws<ContractException>(() => question.SetFileRole(999, ExamFileRole.PublicTest));
        Assert.Throws<ContractException>(() => question.RemoveFile(999));
    }
}
