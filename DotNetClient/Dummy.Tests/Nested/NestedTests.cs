using Dummy.Tests.Infrastructure;
using Guts.Client.NUnit;
using NUnit.Framework;

namespace Dummy.Tests.Nested;

[ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
public class NestedTests
{
    private MockGutsApiServer _backendMock = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _backendMock = MockGutsApiServer.Start();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _backendMock.Dispose();
    }

    [MonitoredTest("A nested test")]
    public void NestedTestShouldAlsoBeFound()
    {
        Assert.Pass();
    }
}