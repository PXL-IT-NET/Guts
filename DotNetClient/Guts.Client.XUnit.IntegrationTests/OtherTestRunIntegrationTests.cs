namespace Guts.Client.XUnit.IntegrationTests;

[ExerciseTestClass("dummyCourse", "dummyChapter", "dummyExercise", "Guts.Client.XUnit/MonitoredFactAttribute.cs")]
public class OtherTestRunIntegrationTests : IClassFixture<BackendFixture>
{
    public OtherTestRunIntegrationTests(BackendFixture fixture)
    {
    }

    [MonitoredFact]
    public void SomeMethod_WithACertainCondition_ShouldResultInSomething()
    {
        Assert.True(true);
    }
}