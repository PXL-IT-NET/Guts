using Guts.Common;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects;

[TestFixture]
public class EmailTests
{
    [TestCase("student@pxl.be")]
    [TestCase("teacher.name@sub.pxl.be")]
    [TestCase("user+tag@domain.com")]
    public void Constructor_ValidEmail_ShouldSetValue(string validValue)
    {
        var email = new Email(validValue);
        Assert.That(email.Value, Is.EqualTo(validValue));
    }

    [TestCase("  student@pxl.be  ", "student@pxl.be")]
    public void Constructor_WithLeadingOrTrailingWhitespace_ShouldTrim(string input, string expected)
    {
        var email = new Email(input);
        Assert.That(email.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_NullOrWhitespace_ShouldThrowContractException(string invalidValue)
    {
        Assert.Throws<ContractException>(() => new Email(invalidValue));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ShouldThrowContractException()
    {
        string tooLong = new string('a', 250) + "@pxl.be";
        Assert.Throws<ContractException>(() => new Email(tooLong));
    }

    [TestCase("plainaddress")]
    [TestCase("@missingusername.com")]
    [TestCase("username@.com")]
    [TestCase("username@domain")]
    [TestCase("username@domain..com")]
    public void Constructor_InvalidFormat_ShouldThrowContractException(string invalidEmail)
    {
        Assert.Throws<ContractException>(() => new Email(invalidEmail));
    }

    [Test]
    public void Equals_CaseInsensitive_ShouldBeEqual()
    {
        var email1 = new Email("Student@PXL.be");
        var email2 = new Email("student@pxl.be");

        Assert.That(email1, Is.EqualTo(email2));
        Assert.That(email1 == email2, Is.True);
        Assert.That(email1.GetHashCode(), Is.EqualTo(email2.GetHashCode()));
    }

    [Test]
    public void ImplicitConversions_ShouldWorkBothWays()
    {
        Email email = "student@pxl.be";
        string stringValue = email;

        Assert.That(email, Is.Not.Null);
        Assert.That(email.Value, Is.EqualTo("student@pxl.be"));
        Assert.That(stringValue, Is.EqualTo("student@pxl.be"));

        Email nullEmail = (string)null;
        Assert.That(nullEmail, Is.Null);
    }
}

