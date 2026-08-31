using Guts.Common.Extensions;

namespace Guts.Common.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [TestCase("SGVsbG8gd29ybGQ=", "Hello world")]
        public void TryConvertFromBase64_ValidBase64String_ReturnsDecodedString(string base64String, string expected)
        {
            // Act
            string result = base64String.TryConvertFromBase64();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestCase("Non Base64String")]
        [TestCase("")]
        public void TryConvertFromBase64_NonBase64String_ReturnsOriginalString(string nonBase64String)
        {
            // Act
            string result = nonBase64String.TryConvertFromBase64();

            // Assert
            Assert.AreEqual(nonBase64String, result);
        }

        [Test]
        public void TryConvertFromBase64_NullString_ReturnsEmptyString()
        {
            // Arrange
            string? nullString = null;

            // Act
            string result = nullString.TryConvertFromBase64();

            // Assert
            Assert.AreEqual(string.Empty, result);
        }
    }
}