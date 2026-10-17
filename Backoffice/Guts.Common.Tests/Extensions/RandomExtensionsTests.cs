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
            Assert.Greater(value, 0);
        }

        [Test]
        public void NextZeroOrNegative_ReturnsZeroOrNegativeIntegerWithinBounds()
        {
            // Act
            int value = Random.Shared.NextZeroOrNegative(-10);

            // Assert
            Assert.LessOrEqual(value, 0);
            Assert.GreaterOrEqual(value, -10);
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
            Assert.IsFalse(string.IsNullOrWhiteSpace(value));
            Assert.IsTrue(Guid.TryParse(value, out _));
        }

        [Test]
        public void NextDateTimeInFuture_ReturnsFutureDateTime()
        {
            // Act
            DateTime value = Random.Shared.NextDateTimeInFuture();

            // Assert
            Assert.Greater(value, DateTime.Now);
        }

        [Test]
        public void NextDateTimeInPast_ReturnsPastDateTime()
        {
            // Act
            DateTime value = Random.Shared.NextDateTimeInPast();

            // Assert
            Assert.Less(value, DateTime.Now);
        }

        [Test]
        public void NextEnum_WithEnumType_ReturnsValidEnumValue()
        {
            // Act
            SampleEnum value = Random.Shared.NextEnum<SampleEnum>();

            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(SampleEnum), value));
        }

        [Test]
        public void NextEnum_WithNonEnumStruct_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Random.Shared.NextEnum<int>());
        }
    }
}

