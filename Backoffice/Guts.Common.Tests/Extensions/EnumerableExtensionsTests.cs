using Guts.Common.Extensions;

namespace Guts.Common.Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Test]
    public void NextRandomItem_NonEmptyCollection_ReturnsElementFromCollection()
    {
        // Arrange
        var items = new[] { "alpha", "beta", "gamma" };

        // Act
        string result = items.NextRandomItem();

        // Assert
        Assert.That(items, Does.Contain(result));
    }
}