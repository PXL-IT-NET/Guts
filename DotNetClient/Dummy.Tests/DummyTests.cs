using Guts.Client.Core;
using NUnit.Framework;

namespace Dummy.Tests
{
    [ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
    public class DummyTests
    {
    //    [MonitoredTest("Test something")]
        public void TestSomething()
        {
            Assert.True(true);
        }
    }
}
