using Guts.Common.Extensions;

namespace Guts.Common.Tests.Extensions
{
    public class RandomExtensionsTests
    {
        private enum SampleEnum
        {
            First,
            Second,
            Third
        }

        [Test]
        public void NextPositive_ReturnsPositiveInteger()
        {
            // Act
            int value = Random.Shared.NextPositive();

            // Assert
            Assert.That(value, Is.GreaterThan(0));
        }

        [Test]
        public void NextZeroOrNegative_ReturnsZeroOrNegativeIntegerWithinBounds()
        {
            // Act
            int value = Random.Shared.NextZeroOrNegative(-10);

            // Assert
            Assert.That(value, Is.LessThanOrEqualTo(0));
            Assert.That(value, Is.GreaterThanOrEqualTo(-10));
        }

        [Test]
        public void NextBool_ReturnsBoolean()
        {
            // Act
            bool value = Random.Shared.NextBool();

            // Assert
            Assert.That(value, Is.True.Or.False);
        }

        [Test]
        public void NextString_ReturnsNonEmptyGuidString()
        {
            // Act
            string value = Random.Shared.NextString();

            // Assert
            Assert.That(string.IsNullOrWhiteSpace(value), Is.False);
            Assert.That(Guid.TryParse(value, out _), Is.True);
        }

        [Test]
        public void NextDateTimeInFuture_ReturnsFutureDateTime()
        {
            // Act
            DateTime value = Random.Shared.NextDateTimeInFuture();

            // Assert
            Assert.That(value, Is.GreaterThan(DateTime.Now));
        }

        [Test]
        public void NextDateTimeInPast_ReturnsPastDateTime()
        {
            // Act
            DateTime value = Random.Shared.NextDateTimeInPast();

            // Assert
            Assert.That(value, Is.LessThan(DateTime.Now));
        }

        [Test]
        public void NextEnum_WithEnumType_ReturnsValidEnumValue()
        {
            // Act
            SampleEnum value = Random.Shared.NextEnum<SampleEnum>();

            // Assert
            Assert.That(Enum.IsDefined(typeof(SampleEnum), value), Is.True);
        }

        [Test]
        public void NextEnum_WithNonEnumStruct_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Random.Shared.NextEnum<int>());
        }
    }
}
