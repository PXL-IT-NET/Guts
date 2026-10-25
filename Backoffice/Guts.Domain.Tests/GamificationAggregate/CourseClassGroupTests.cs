using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate
{
    [TestFixture]
    public class CourseClassGroupTests
    {
        [Test]
        public void Constructor_ValidArguments_ShouldSetProperties()
        {
            var group = new CourseClassGroup("1TIN-A", 10, 20);
            Assert.That(group.Name, Is.EqualTo("1TIN-A"));
            Assert.That(group.CourseId, Is.EqualTo(10));
            Assert.That(group.PeriodId, Is.EqualTo(20));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Constructor_InvalidName_ShouldThrowContractException(string invalidName)
        {
            Assert.Throws<ContractException>(() => new CourseClassGroup(invalidName, 10, 20));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidCourseId_ShouldThrowContractException(int invalidCourseId)
        {
            Assert.Throws<ContractException>(() => new CourseClassGroup("1TIN-A", invalidCourseId, 20));
        }

        [Test]
        public void Name_Setter_Valid_ShouldTrimAndSet()
        {
            var group = new CourseClassGroup("1TIN-A", 10, 20);
            group.Name = "  2TIN-B  ";
            Assert.That(group.Name, Is.EqualTo("2TIN-B"));
        }
    }
}

