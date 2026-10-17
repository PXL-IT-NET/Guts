using System.IO;
using System.Linq;
using Guts.Common;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class FilePathTests
    {
        [Test]
        public void Constructor_ValidFilePath_ShouldSetPropertiesCorrectly()
        {
            string rawPath = Path.Combine("Folder", "SubFolder", "MyClass.cs");

            var filePath = new FilePath(rawPath);

            Assert.That(filePath.FullPath, Is.EqualTo(rawPath));
            Assert.That(filePath.BasePath, Is.EqualTo(Path.Combine("Folder", "SubFolder")));
            Assert.That(filePath.BaseName, Is.EqualTo("MyClass"));
            Assert.That(filePath.Extension, Is.EqualTo(".cs"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Constructor_NullOrEmptyPath_ShouldThrowContractException(string invalidPath)
        {
            Assert.That(() => new FilePath(invalidPath), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void Constructor_PathWithInvalidCharacters_ShouldThrowContractException()
        {
            char invalidChar = Path.GetInvalidPathChars().First();
            string invalidPath = $"Folder{invalidChar}File.cs";

            Assert.That(() => new FilePath(invalidPath), Throws.InstanceOf<ContractException>());
        }

        [TestCase("FileWithoutExtension")]
        [TestCase("Folder/FileWithoutExtension")]
        [TestCase("Folder/FileWithTrailingDot.")]
        public void Constructor_MissingFileExtension_ShouldThrowContractException(string pathWithoutExtension)
        {
            Assert.That(() => new FilePath(pathWithoutExtension), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void ImplicitConversion_StringToAndFromFilePath_ShouldPreserveFullPath()
        {
            string rawPath = "Domain/User.cs";

            FilePath filePath = rawPath;
            string convertedBack = filePath;

            Assert.That(filePath.FullPath, Is.EqualTo(rawPath));
            Assert.That(convertedBack, Is.EqualTo(rawPath));
        }

        [Test]
        public void Equality_SameFullPath_ShouldBeEqual()
        {
            var pathA = new FilePath("Folder/Program.cs");
            var pathB = new FilePath("Folder/Program.cs");

            Assert.That(pathA.Equals(pathB), Is.True);
            Assert.That(pathA == pathB, Is.True);
            Assert.That(pathA != pathB, Is.False);
            Assert.That(pathA.GetHashCode(), Is.EqualTo(pathB.GetHashCode()));
        }

        [Test]
        public void Equality_DifferentFullPath_ShouldNotBeEqual()
        {
            var pathA = new FilePath("Folder/ProgramA.cs");
            var pathB = new FilePath("Folder/ProgramB.cs");

            Assert.That(pathA.Equals(pathB), Is.False);
            Assert.That(pathA == pathB, Is.False);
            Assert.That(pathA != pathB, Is.True);
            Assert.That(pathA.GetHashCode(), Is.Not.EqualTo(pathB.GetHashCode()));
        }

        [Test]
        public void ToString_ShouldReturnFullPath()
        {
            string rawPath = "Src/App.java";
            var filePath = new FilePath(rawPath);

            Assert.That(filePath.ToString(), Is.EqualTo(rawPath));
        }
    }
}
