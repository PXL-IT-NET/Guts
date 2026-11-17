using Guts.Common;
using Guts.Domain.ExamAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamAccessCodeTests
{
    [Test]
    public void Constructor_ValidCode_TrimsAndStoresValue()
    {
        var code = new ExamAccessCode("  EXAM-2026  ");

        Assert.That(code.Value, Is.EqualTo("EXAM-2026"));
        Assert.That(code.ToString(), Is.EqualTo("EXAM-2026"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("abc")]
    [TestCase("  a  ")]
    [TestCase("123456789012345678901234567890123")]
    public void Constructor_InvalidCode_ThrowsContractException(string invalidCode)
    {
        Assert.Throws<ContractException>(() => _ = new ExamAccessCode(invalidCode));
    }

    [Test]
    public void Matches_ComparesTrimmedValueInConstantTime()
    {
        var code = new ExamAccessCode("Secret42");

        Assert.That(code.Matches("Secret42"), Is.True);
        Assert.That(code.Matches("  Secret42  "), Is.True);
        Assert.That(code.Matches("secret42"), Is.False);
        Assert.That(code.Matches("Wrong"), Is.False);
        Assert.That(code.Matches(null), Is.False);
        Assert.That(code.Matches("   "), Is.False);
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var first = new ExamAccessCode("PXL-1234");
        var second = new ExamAccessCode("  PXL-1234 ");
        var different = new ExamAccessCode("PXL-9999");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(different));
    }
}
