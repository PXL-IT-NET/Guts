using Guts.Api.Mcp.Prompts;
using NUnit.Framework;

namespace Guts.Api.Tests.Mcp.Prompts;

public class GutsTestPromptsTests
{
    [Test]
    public void CreateGutsTestsForExercise_ShouldContainExerciseSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsTestsForExercise("OrderService", "dotNet1", "exercise3");

        Assert.That(result, Does.Contain("OrderService"));
        Assert.That(result, Does.Contain("ExerciseTestFixture"));
        Assert.That(result, Does.Contain("ExerciseTestClass"));
        Assert.That(result, Does.Contain("dotNet1"));
        Assert.That(result, Does.Contain("exercise3"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }

    [Test]
    public void CreateGutsTestsForProjectComponent_ShouldContainProjectComponentSpecificInstructions()
    {
        var prompts = new GutsTestPrompts();

        var result = prompts.CreateGutsTestsForProjectComponent("InvoiceCalculator", "dotNet2", "componentA");

        Assert.That(result, Does.Contain("InvoiceCalculator"));
        Assert.That(result, Does.Contain("ProjectComponentTestFixture"));
        Assert.That(result, Does.Contain("ProjectComponentTestClass"));
        Assert.That(result, Does.Contain("dotNet2"));
        Assert.That(result, Does.Contain("componentA"));
        Assert.That(result, Does.Contain("guts://guides/nunit-attributes"));
        Assert.That(result, Does.Contain("guts://guides/xunit-attributes"));
    }
}
