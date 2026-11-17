using Guts.Common;
using Guts.Domain.ExamAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamFilePathTests
{
    [Test]
    public void Constructor_NormalizesSlashesAndLeadingDotSlash()
    {
        var path = new ExamFilePath(@"  .\src\Models\Order.cs  ");

        Assert.That(path.Value, Is.EqualTo("src/Models/Order.cs"));
        Assert.That(path.ToString(), Is.EqualTo("src/Models/Order.cs"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("./")]
    [TestCase("/root/Program.cs")]
    [TestCase(@"\root\Program.cs")]
    [TestCase("../Secret.cs")]
    [TestCase("src/../../Secret.cs")]
    [TestCase("src//Program.cs")]
    [TestCase("bin/Debug/App.dll")]
    [TestCase("MyApp/OBJ/Program.cs")]
    [TestCase(".git/config")]
    public void Constructor_InvalidOrDisallowedPath_ThrowsContractException(string invalidPath)
    {
        Assert.Throws<ContractException>(() => _ = new ExamFilePath(invalidPath));
    }

    [Test]
    public void Constructor_ExceedingMaxLength_ThrowsContractException()
    {
        var tooLong = new string('a', 258) + ".cs";
        Assert.Throws<ContractException>(() => _ = new ExamFilePath(tooLong));
    }

    [Test]
    public void Equality_NormalizedValue_WorksCorrectly()
    {
        var first = new ExamFilePath(@"src\Program.cs");
        var second = new ExamFilePath("./src/Program.cs");
        var third = new ExamFilePath("src/Other.cs");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(third));
    }
}
