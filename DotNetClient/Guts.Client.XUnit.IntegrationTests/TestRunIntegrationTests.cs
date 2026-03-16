namespace Guts.Client.XUnit.IntegrationTests;

[ExerciseTestClass("dummyCourse", "dummyChapter", "dummyExercise", "Guts.Client.XUnit/MonitoredFactAttribute.cs")]
public class TestRunIntegrationTests : IClassFixture<BackendFixture>
{
    public TestRunIntegrationTests(BackendFixture fixture)
    {
    }

    [MonitoredFact]
    public void SomeMethod_WithACertainCondition_ShouldResultInSomething()
    {
        Assert.True(true);
    }

    [MonitoredFact("Custom test name")]
    public void SomeMethod_WithACustomName()
    {
        Assert.True(true);
    }

    [MonitoredTheory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void TestWithCases(int a, int b)
    {
        Assert.True(a < b);
    }
}