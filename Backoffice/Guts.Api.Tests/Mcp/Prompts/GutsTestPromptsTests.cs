using Guts.Api.Mcp.Prompts;
using NUnit.Framework;

namespace Guts.Api.Tests.Mcp.Prompts;

public class GutsTestPromptsTests
{
    [Test]
    public void CreateGutsXUnitTestsForExercise_ShouldContainExerciseSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsXUnitTestsForExercise("OrderService");

        Assert.That(result, Does.Contain("OrderService"));
        Assert.That(result, Does.Contain("ExerciseTestFixture"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_chapterCode"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }

    [Test]
    public void CreateGutsNUnitTestsForExercise_ShouldContainExerciseSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsNUnitTestsForExercise("OrderService");

        Assert.That(result, Does.Contain("OrderService"));
        Assert.That(result, Does.Contain("ExerciseTestFixture"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_chapterCode"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
    }

    [Test]
    public void CreateGutsXUnitTestsForProjectComponent_ShouldContainProjectComponentSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsXUnitTestsForProjectComponent("InvoiceCalculator");

        Assert.That(result, Does.Contain("InvoiceCalculator"));
        Assert.That(result, Does.Contain("ProjectComponentTestFixture"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_projectCode"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }

    [Test]
    public void CreateGutsNUnitTestsForProjectComponent_ShouldContainProjectComponentSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsNUnitTestsForProjectComponent("InvoiceCalculator");

        Assert.That(result, Does.Contain("InvoiceCalculator"));
        Assert.That(result, Does.Contain("ProjectComponentTestFixture"));
        Assert.That(result, Does.Contain("TODO_courseCode"));
        Assert.That(result, Does.Contain("TODO_projectCode"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
    }
}
