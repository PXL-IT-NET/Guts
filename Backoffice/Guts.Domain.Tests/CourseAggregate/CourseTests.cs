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
}

