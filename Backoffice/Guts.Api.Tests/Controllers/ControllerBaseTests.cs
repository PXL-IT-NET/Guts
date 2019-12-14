using System.Security.Authentication;
using Guts.Api.Controllers;
using Guts.Api.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class ControllerBaseTests
    {
        private TestableControllerBase _controller;

        [SetUp]
        public void Setup()
        {
            _controller = new TestableControllerBase();    
        }

        [Test]
        public void UserIdShouldReturnMinusOneIfNotAuthenticated()
        {
            //Act
            var userId = _controller.GetUserId();

            //Assert
            Assert.That(userId, Is.EqualTo(-1));
        }

        [Test]
        public void UserIdShouldThrowExceptionWhenNameIdentifierClaimIsNotPresent()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithUserWithoutNameIdentifier().Build();

            //Act + Assert
            Assert.That(() => _controller.GetUserId(), Throws.TypeOf<AuthenticationException>());
        }

        [Test]
        [TestCase("notANumber")]
        [TestCase("0")]
        [TestCase("-1")]
        public void UserIdShouldThrowExceptionWhenNameIdentifierClaimIsNotAPositiveInteger(string invalidNameIdentifier)
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithUser(invalidNameIdentifier).Build();

            //Act + Assert
            Assert.That(() => _controller.GetUserId(), Throws.TypeOf<AuthenticationException>());
        }

        private class TestableControllerBase : ControllerBase
        {
            public new int GetUserId()
            {
                return base.GetUserId();
            }
        }
    }
}
