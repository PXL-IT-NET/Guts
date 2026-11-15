using Guts.Common;
using Guts.Domain.CourseAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.CourseAggregate;

[TestFixture]
public class CourseTests
{
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Constructor_InvalidCode_ThrowsContractException(string invalidCode)
    {
        Assert.Throws<ContractException>(() => new Course(invalidCode, "Valid Name"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Constructor_InvalidName_ThrowsContractException(string invalidName)
    {
        Assert.Throws<ContractException>(() => new Course("CS101", invalidName));
    }

    [Test]
    public void Constructor_ValidArguments_InitializesAndTrimsProperties()
    {
        var course = new Course("  CS101  ", "  Intro to C#  ");

        Assert.That(course.Code, Is.EqualTo("CS101"));
        Assert.That(course.Name, Is.EqualTo("Intro to C#"));
    }

    [Test]
    public void Constructor_CodeExceeds20Chars_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() => new Course(new string('A', 21), "Valid Name"));
    }

    [Test]
    public void Update_ValidArguments_UpdatesAndTrimsProperties()
    {
        var course = new Course("CS101", "Intro to C#");

        course.Update("  CS102  ", "  Advanced C#  ");

        Assert.That(course.Code, Is.EqualTo("CS102"));
        Assert.That(course.Name, Is.EqualTo("Advanced C#"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Update_InvalidCode_ThrowsContractException(string invalidCode)
    {
        var course = new Course("CS101", "Intro to C#");

        Assert.Throws<ContractException>(() => course.Update(invalidCode, "Valid Name"));
    }

    [Test]
    public void Update_CodeExceeds20Chars_ThrowsContractException()
    {
        var course = new Course("CS101", "Intro to C#");

        Assert.Throws<ContractException>(() => course.Update(new string('A', 21), "Valid Name"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Update_InvalidName_ThrowsContractException(string invalidName)
    {
        var course = new Course("CS101", "Intro to C#");

        Assert.Throws<ContractException>(() => course.Update("CS101", invalidName));
    }

    [Test]
    public void Factory_CreateNew_UniqueCode_ReturnsCourse()
    {
        var factory = new Course.Factory();
        var existing = new[] { new Course("CS101", "Intro") { Id = 1 } };

        var created = factory.CreateNew("  CS102 ", " Advanced ", existing);

        Assert.That(created.Code, Is.EqualTo("CS102"));
        Assert.That(created.Name, Is.EqualTo("Advanced"));
    }

    [Test]
    public void Factory_CreateNew_DuplicateCodeCaseInsensitive_ThrowsContractException()
    {
        var factory = new Course.Factory();
        var existing = new[] { new Course("CS101", "Intro") { Id = 1 } };

        Assert.Throws<ContractException>(() => factory.CreateNew("cs101", "Other", existing));
    }
}

