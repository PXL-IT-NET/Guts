namespace Guts.Common.Tests
{
    public class ContractsTests
    {
        [Test]
        public void Require_PreconditionTrue_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Contracts.Require(true, "Should not fail"));
        }

        [Test]
        public void Require_PreconditionFalse_ThrowsContractExceptionWithMessage()
        {
            // Arrange
            string expectedMessage = "Precondition failed";

            // Act & Assert
            var exception = Assert.Throws<ContractException>(() => Contracts.Require(false, expectedMessage));
            Assert.That(exception!.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void Require_WithMessageFactory_PreconditionTrue_DoesNotInvokeFactoryOrThrow()
        {
            // Arrange
            bool factoryInvoked = false;

            // Act & Assert
            Assert.DoesNotThrow(() => Contracts.Require(true, () =>
            {
                factoryInvoked = true;
                return "Should not fail";
            }));
            Assert.That(factoryInvoked, Is.False);
        }

        [Test]
        public void Require_WithMessageFactory_PreconditionFalse_ThrowsContractExceptionWithFactoryMessage()
        {
            // Arrange
            string expectedMessage = "Lazy precondition failed";

            // Act & Assert
            var exception = Assert.Throws<ContractException>(() => Contracts.Require(false, () => expectedMessage));
            Assert.That(exception!.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void ContractException_Constructors_InitializePropertiesCorrectly()
        {
            // Default constructor
            var defaultEx = new ContractException();
            Assert.That(defaultEx.Message, Is.Not.Null);

            // Message constructor
            var msgEx = new ContractException("Custom error");
            Assert.That(msgEx.Message, Is.EqualTo("Custom error"));

            // Message + inner exception constructor
            var inner = new InvalidOperationException("Inner");
            var wrappedEx = new ContractException("Outer", inner);
            Assert.That(wrappedEx.Message, Is.EqualTo("Outer"));
            Assert.That(wrappedEx.InnerException, Is.SameAs(inner));
        }
    }
}
