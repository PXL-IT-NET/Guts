using Guts.Api.Mcp.Resources;
using Guts.Api.Mcp.Services;
using Guts.Common.Extensions;
using Moq;
using NUnit.Framework;
using System;

namespace Guts.Api.Tests.Mcp.Resources;

[TestFixture]
public class GutsGuideResourcesTests
{
    private Mock<IAttributeGuideService> _guideServiceMock;
    private AttributeGuideResources _resources;

    [SetUp]
    public void Setup()
    {
        _guideServiceMock = new Mock<IAttributeGuideService>();
        _resources = new AttributeGuideResources(_guideServiceMock.Object);
    }

    [Test]
    public void DescribeNUnitAttributes_ShouldDelegateToAttributeGuideService()
    {
        // Arrange
        string expectedGuide = Random.Shared.NextString();
        _guideServiceMock.Setup(service => service.GetNUnitGuide()).Returns(expectedGuide);

        // Act
        string actualGuide = _resources.DescribeNUnitAttributes();

        // Assert
        Assert.That(actualGuide, Is.EqualTo(expectedGuide));
        _guideServiceMock.Verify(service => service.GetNUnitGuide(), Times.Once);
    }

    [Test]
    public void DescribeXUnitAttributes_ShouldDelegateToAttributeGuideService()
    {
        // Arrange
        string expectedGuide = Random.Shared.NextString();
        _guideServiceMock.Setup(service => service.GetXUnitGuide()).Returns(expectedGuide);

        // Act
        string actualGuide = _resources.DescribeXUnitAttributes();

        // Assert
        Assert.That(actualGuide, Is.EqualTo(expectedGuide));
        _guideServiceMock.Verify(service => service.GetXUnitGuide(), Times.Once);
    }
}
