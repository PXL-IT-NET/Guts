using Guts.Client.Core;
using NUnit.Framework;

namespace Dummy.Tests.Nested
{
    [ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
    [Ignore("These tests could actually send testresults")]
    public class NestedTests
    {
        [MonitoredTest("A nested test")]
        public void NestedTestShouldAlsoBeFound()
        {
            Assert.Pass();
        }
    }
}
