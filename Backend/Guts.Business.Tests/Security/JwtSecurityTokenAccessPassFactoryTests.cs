using System;
using System.Collections.Generic;
using System.Security.Claims;
using Guts.Business.Security;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Business.Tests.Security
{
    [TestFixture]
    public class JwtSecurityTokenAccessPassFactoryTests
    {
        [Test]
        public void Create_ShouldReturnAccessPassWithTokenAndValidExpirationDate()
        {
            //Arrange
            string key = Guid.NewGuid().ToString();
            string issuer = Guid.NewGuid().ToString();
            string audience = Guid.NewGuid().ToString();
            var random = new Random();
            int expirationTimeInMinutes = random.NextPositive();
            var factory = new JwtSecurityTokenAccessPassFactory(key, issuer, audience, expirationTimeInMinutes);
            var user = new User{ Id = random.NextPositive(), UserName = Guid.NewGuid().ToString(), Email = "someEmail@test.com"};
            var claims = new List<Claim>();
            var roles = new List<string>();
            var expectedExpirationDate = DateTime.UtcNow.AddMinutes(expirationTimeInMinutes);

            //Act
            var accessPass = factory.Create(user, claims, roles);

            //Assert
            Assert.That(accessPass.Token, Is.Not.Null.Or.Empty);
            Assert.That(accessPass.Expiration, Is.EqualTo(expectedExpirationDate).Within(20).Seconds);
        }
    }
}
