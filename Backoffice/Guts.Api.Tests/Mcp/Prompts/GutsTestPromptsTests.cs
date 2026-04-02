using Guts.Api.Mcp.Prompts;
using NUnit.Framework;

namespace Guts.Api.Tests.Mcp.Prompts;

public class GutsTestPromptsTests
{
    [Test]
    public void CreateGutsTests_ForExercise_ShouldContainExerciseSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsTests("OrderService", isIndividualExercise: true);

        Assert.That(result, Does.Contain("OrderService"));
        Assert.That(result, Does.Contain("ExerciseTestFixture"));
        Assert.That(result, Does.Contain("ExerciseTestClass"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_chapterCode"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }

    [Test]
    public void CreateGutsTests_ForProjectComponent_ShouldContainProjectComponentSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsTests("InvoiceCalculator", isIndividualExercise: false);

        Assert.That(result, Does.Contain("InvoiceCalculator"));
        Assert.That(result, Does.Contain("ProjectComponentTestFixture"));
        Assert.That(result, Does.Contain("ProjectComponentTestClass"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_projectCode"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }
}
