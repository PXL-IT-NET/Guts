using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Guts.Api.Controllers;
using Guts.Api.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.RoleAggregate;
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
        public void UserIdShouldReturnPositiveIntegerWhenNameIdentifierClaimIsValid()
        {
            //Arrange
            int expectedUserId = Random.Shared.NextPositive();
            _controller.ControllerContext = new ControllerContextBuilder().WithUser(expectedUserId.ToString()).Build();

            //Act
            var userId = _controller.GetUserId();

            //Assert
            Assert.That(userId, Is.EqualTo(expectedUserId));
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

        [Test]
        public void GetUserEmail_WhenEmailClaimIsPresent_ShouldReturnEmail()
        {
            //Arrange
            string expectedEmail = "user@pxl.be";
            _controller.ControllerContext = new ControllerContextBuilder().WithEmail(expectedEmail).Build();

            //Act
            string email = _controller.GetUserEmail();

            //Assert
            Assert.That(email, Is.EqualTo(expectedEmail));
        }

        [Test]
        public void GetUserEmail_WhenUserIsNull_ShouldReturnEmptyString()
        {
            //Act
            string email = _controller.GetUserEmail();

            //Assert
            Assert.That(email, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetUserEmail_WhenEmailClaimIsMissing_ShouldReturnEmptyString()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithUser("1").Build();

            //Act
            string email = _controller.GetUserEmail();

            //Assert
            Assert.That(email, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetUserRoles_ShouldReturnAllRoleClaims()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder()
                .WithRole(Role.Constants.Student)
                .WithRole(Role.Constants.Lector)
                .Build();

            //Act
            IList<string> roles = _controller.GetUserRoles();

            //Assert
            Assert.That(roles, Has.Count.EqualTo(2));
            Assert.That(roles, Does.Contain(Role.Constants.Student));
            Assert.That(roles, Does.Contain(Role.Constants.Lector));
        }

        [Test]
        public void IsStudent_WhenUserHasStudentRole_ShouldReturnTrue()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithRole(Role.Constants.Student).Build();

            //Act + Assert
            Assert.That(_controller.IsStudent(), Is.True);
        }

        [Test]
        public void IsStudent_WhenUserDoesNotHaveStudentRole_ShouldReturnFalse()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithRole(Role.Constants.Lector).Build();

            //Act + Assert
            Assert.That(_controller.IsStudent(), Is.False);
        }

        [Test]
        public void IsStudent_WhenUserIsNull_ShouldReturnFalse()
        {
            //Act + Assert
            Assert.That(_controller.IsStudent(), Is.False);
        }

        [Test]
        public void IsLector_WhenUserHasLectorRole_ShouldReturnTrue()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithRole(Role.Constants.Lector).Build();

            //Act + Assert
            Assert.That(_controller.IsLector(), Is.True);
        }

        [Test]
        public void IsLector_WhenUserDoesNotHaveLectorRole_ShouldReturnFalse()
        {
            //Arrange
            _controller.ControllerContext = new ControllerContextBuilder().WithRole(Role.Constants.Student).Build();

            //Act + Assert
            Assert.That(_controller.IsLector(), Is.False);
        }

        [Test]
        public void IsLector_WhenUserIsNull_ShouldReturnFalse()
        {
            //Act + Assert
            Assert.That(_controller.IsLector(), Is.False);
        }

        [Test]
        public void IsOwnUserId_WhenUserIdMatchesCaller_ShouldReturnTrue()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act + Assert
            Assert.That(_controller.IsOwnUserId(userId), Is.True);
        }

        [Test]
        public void IsOwnUserId_WhenUserIdDoesNotMatchCaller_ShouldReturnFalse()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act + Assert
            Assert.That(_controller.IsOwnUserId(userId + 1), Is.False);
        }

        private class TestableControllerBase : ControllerBase
        {
            public new int GetUserId()
            {
                return base.GetUserId();
            }

            public new string GetUserEmail()
            {
                return base.GetUserEmail();
            }

            public new IList<string> GetUserRoles()
            {
                return base.GetUserRoles();
            }

            public new bool IsStudent()
            {
                return base.IsStudent();
            }

            public new bool IsLector()
            {
                return base.IsLector();
            }

            public new bool IsOwnUserId(int userId)
            {
                return base.IsOwnUserId(userId);
            }
        }
    }
}
