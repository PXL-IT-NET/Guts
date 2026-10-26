using Guts.Common.Extensions;

namespace Guts.Common.Tests.Extensions;

public class StringExtensionsTests
{
    [TestCase("SGVsbG8gd29ybGQ=", "Hello world")]
    public void TryConvertFromBase64_ValidBase64String_ReturnsDecodedString(string base64String, string expected)
    {
        // Act
        string result = base64String.TryConvertFromBase64();

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Non Base64String")]
    [TestCase("")]
    public void TryConvertFromBase64_NonBase64String_ReturnsOriginalString(string nonBase64String)
    {
        // Act
        string result = nonBase64String.TryConvertFromBase64();

        // Assert
        Assert.That(result, Is.EqualTo(nonBase64String));
    }

    [Test]
    public void TryConvertFromBase64_NullString_ReturnsEmptyString()
    {
        // Arrange
        string? nullString = null;

        // Act
        string result = nullString.TryConvertFromBase64();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToValidFilePath_NullOrEmpty_ReturnsTripleUnderscore(string? input)
    {
        // Act
        string result = input!.ToValidFilePath();

        // Assert
        Assert.That(result, Is.EqualTo("___"));
    }

    [TestCase("  my file name.cs  ", "my_file_name.cs")]
    [TestCase("folder/sub folder/file.txt", "folder/sub_folder/file.txt")]
    public void ToValidFilePath_TrimsAndReplacesSpacesWithUnderscores(string input, string expected)
    {
        // Act
        string result = input.ToValidFilePath();

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToValidFilePath_ContainsInvalidPathChars_ReplacesInvalidCharsWithUnderscores()
    {
        // Arrange
        char invalidChar = Path.GetInvalidPathChars().First();
        string input = $"folder{invalidChar}file.cs";

        // Act
        string result = input.ToValidFilePath();

        // Assert
        Assert.That(result, Is.EqualTo("folder_file.cs"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void IsValidFilePath_NullOrEmpty_ReturnsFalse(string? input)
    {
        // Act
        bool result = input!.IsValidFilePath();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValidFilePath_ContainsInvalidPathChar_ReturnsFalse()
    {
        // Arrange
        char invalidChar = Path.GetInvalidPathChars().First();
        string input = $"folder{invalidChar}file.cs";

        // Act
        bool result = input.IsValidFilePath();

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("folder/file.cs")]
    [TestCase("MyClass.cs")]
    public void IsValidFilePath_ValidPath_ReturnsTrue(string input)
    {
        // Act
        bool result = input.IsValidFilePath();

        // Assert
        Assert.That(result, Is.True);
    }
}