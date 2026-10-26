using Guts.Common;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects;

[TestFixture]
public class CodeTests
{
    [TestCase("Chapter_01")]
    [TestCase("Ex-1.2")]
    [TestCase("ABC123_def-456.789")]
    public void Constructor_ValidCode_ShouldSetValue(string validValue)
    {
        var code = new Code(validValue);

        Assert.That(code.Value, Is.EqualTo(validValue));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Constructor_NullOrEmpty_ShouldThrowContractException(string invalidValue)
    {
        Assert.That(() => new Code(invalidValue), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ShouldThrowContractException()
    {
        string maxLengthCode = new string('a', Code.MaxLength);
        string tooLongCode = new string('a', Code.MaxLength + 1);

        Assert.That(() => new Code(maxLengthCode), Throws.Nothing);
        Assert.That(() => new Code(tooLongCode), Throws.InstanceOf<ContractException>());
    }

    [TestCase("   ")]
    [TestCase("Code With Spaces")]
    [TestCase("Code@1")]
    [TestCase("Code/1")]
    [TestCase("Code#1")]
    [TestCase(" leadingSpace")]
    [TestCase("trailingSpace ")]
    public void Constructor_InvalidCharacters_ShouldThrowContractException(string invalidValue)
    {
        Assert.That(() => new Code(invalidValue), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void ImplicitConversion_StringToAndFromCode_ShouldPreserveValue()
    {
        string raw = "Project_2026-Part.1";

        Code code = raw;
        string convertedBack = code;

        Assert.That(code.Value, Is.EqualTo(raw));
        Assert.That(convertedBack, Is.EqualTo(raw));
    }

    [Test]
    public void ImplicitConversion_NullCodeToString_ShouldReturnNull()
    {
        Code nullCode = null;

        string converted = nullCode;

        Assert.That(converted, Is.Null);
    }

    [Test]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var codeA = new Code("Ex_01");
        var codeB = new Code("Ex_01");

        Assert.That(codeA.Equals(codeB), Is.True);
        Assert.That(codeA == codeB, Is.True);
        Assert.That(codeA != codeB, Is.False);
        Assert.That(codeA.GetHashCode(), Is.EqualTo(codeB.GetHashCode()));
    }

    [Test]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var codeA = new Code("Ex_01");
        var codeB = new Code("Ex_02");

        Assert.That(codeA.Equals(codeB), Is.False);
        Assert.That(codeA == codeB, Is.False);
        Assert.That(codeA != codeB, Is.True);
        Assert.That(codeA.GetHashCode(), Is.Not.EqualTo(codeB.GetHashCode()));
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        var code = new Code("Test.Code-1");

        Assert.That(code.ToString(), Is.EqualTo("Test.Code-1"));
    }
}