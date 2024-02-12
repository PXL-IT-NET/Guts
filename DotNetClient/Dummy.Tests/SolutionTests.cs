using Guts.Client.Core;
using Guts.Client.Core.TestTools;
using NUnit.Framework;

namespace Dummy.Tests;

[ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
[Ignore("These tests could actually send testresults")]
public class SolutionTests
{
    [MonitoredTest]
    public void SomeMethod_WithACertainCondition_ShouldResultInSomething()
    {
        string sourceCode = Solution.Current.GetFileContent(@"Guts.Client.Core\TestTools\Solution.cs");
        Assert.That(sourceCode, Is.Not.Empty);
    }
}