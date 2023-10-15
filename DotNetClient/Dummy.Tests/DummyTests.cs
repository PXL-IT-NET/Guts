using Guts.Client.Core;
using NUnit.Framework;

namespace Dummy.Tests
{
    [ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
    [Ignore("These tests could actually send testresults")]
    public class DummyTests
    {
        [MonitoredTest("Test something")]
        public void TestSomething()
        {
            Assert.True(true);
        }

        [MonitoredTest("Test with cases")]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void TestWithCases(int a, int b)
        {
            Assert.True(a < b);
        }
    }
}
