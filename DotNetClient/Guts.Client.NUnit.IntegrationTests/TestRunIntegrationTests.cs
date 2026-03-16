using Guts.Client.NUnit.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace Guts.Client.NUnit.IntegrationTests;

[ExerciseTestFixture("dummyCourse", "dummyChapter", "dummyExercise")]
public class TestRunIntegrationTests
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