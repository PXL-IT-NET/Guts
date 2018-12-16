using Guts.Client.Classic;
using Guts.Client.Shared;
using NUnit.Framework;

namespace Dummy.Tests
{
    [ExerciseTestFixture("dummyCourse", 1, "dummyExercise")]
    public class DummyTests
    {
        [MonitoredTest("Test something")]
        public void TestSomething()
        {
            Assert.True(true);
        }
    }
}
