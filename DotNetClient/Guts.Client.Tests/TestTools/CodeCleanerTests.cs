using Guts.Client.TestTools;
using NUnit.Framework;

namespace Guts.Client.Tests.TestTools
{
    [TestFixture]
    public class CodeCleanerTests
    {
        [Test]
        [TestCase("var a = 3;/*Block \n comment*/var b = 3;", "var a = 3;var b = 3;")]
        [TestCase("var a = 3;/*Block //nested comment\n comment*/var b = 3;", "var a = 3;var b = 3;")]
        public void StripCommentsShouldRemoveBlockComments(string originalCode, string expectedCode)
        {
            TestStripComments(originalCode, expectedCode);
        }

        [Test]
        [TestCase("var a = 1;\r\n//last line is comment", "var a = 1;\r\n")]
        [TestCase("//commented line\r\nvar a = 2;", "var a = 2;")]
        [TestCase("var a = 3;//Comment after code\nvar b = 3;", "var a = 3;var b = 3;")]
        public void StripCommentsShouldRemoveLineComments(string originalCode, string expectedCode)
        {
            TestStripComments(originalCode, expectedCode);
        }

        private static void TestStripComments(string originalCode, string expectedCode)
        {
            var cleanedCode = CodeCleaner.StripComments(originalCode);
            Assert.That(cleanedCode, Is.EqualTo(expectedCode));
        }
    }
}
