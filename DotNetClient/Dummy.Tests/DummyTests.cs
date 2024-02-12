using Guts.Client.Core;
using NUnit.Framework;

namespace Dummy.Tests
{
    [ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
    [Ignore("These tests could actually send testresults")]
    public class DummyTests
    {
        [MonitoredTest]
        public void SomeMethod_WithACertainCondition_ShouldResultInSomething()
        {
            Assert.Pass();
        }

        [MonitoredTest("Test with cases")]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void TestWithCases(int a, int b)
        {
            Assert.That(a < b, Is.True);
        }
    }
}
