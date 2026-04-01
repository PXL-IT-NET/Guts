using Guts.Api.Mcp.Services;
using NUnit.Framework;

namespace Guts.Api.Tests.Mcp.Resources;

public class AttributeGuideServiceTests
{
    [Test]
    public void GetNUnitGuide_ShouldContainRequiredNUnitAttributesAndFlow()
    {
        var service = new AttributeGuideService();

        var guide = service.GetNUnitGuide();

        Assert.That(guide, Does.Contain("ExerciseTestFixture"));
        Assert.That(guide, Does.Contain("ProjectComponentTestFixture"));
        Assert.That(guide, Does.Contain("MonitoredTest"));
        Assert.That(guide, Does.Contain("TestCase"));
    }

    [Test]
    public void GetXUnitGuide_ShouldContainRequiredXUnitAttributesAndFlow()
    {
        var service = new AttributeGuideService();

        var guide = service.GetXUnitGuide();

        Assert.That(guide, Does.Contain("ExerciseTestClass"));
        Assert.That(guide, Does.Contain("ProjectComponentTestClass"));
        Assert.That(guide, Does.Contain("MonitoredFact"));
        Assert.That(guide, Does.Contain("MonitoredTheory"));
        Assert.That(guide, Does.Contain("InlineData"));
    }
}
