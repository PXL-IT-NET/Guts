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

        [Test]
        public void DefaultConstructor_And_NavigationProperties_ShouldWork()
        {
            var group = new CourseClassGroup
            {
                Name = "1TIN-C",
                CourseId = 5,
                PeriodId = 6,
                Course = new Guts.Domain.CourseAggregate.Course { Id = 5 },
                Period = new Guts.Domain.Tests.Builders.PeriodBuilder().WithId().Build()
            };

            Assert.That(group.Name, Is.EqualTo("1TIN-C"));
            Assert.That(group.Course.Id, Is.EqualTo(5));
            Assert.That(group.Period, Is.Not.Null);
        }

        [Test]
        public void Factory_CreateNew_ShouldCreateCourseClassGroup()
        {
            var factory = new CourseClassGroup.Factory();
            var group = factory.CreateNew("1TIN-D", 10, 20);

            Assert.That(group, Is.Not.Null);
            Assert.That(group.Name, Is.EqualTo("1TIN-D"));
            Assert.That(group.CourseId, Is.EqualTo(10));
            Assert.That(group.PeriodId, Is.EqualTo(20));
        }
    }
}
