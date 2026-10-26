using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Guts.Business.Security;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Business.Tests.Security;

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

    [Test]
    public void Create_WithClaimsAndRoles_ShouldIncludeUserClaimsAndRoleClaimsInJwtToken()
    {
        //Arrange
        string key = Guid.NewGuid().ToString();
        string issuer = Guid.NewGuid().ToString();
        string audience = Guid.NewGuid().ToString();
        int expirationTimeInMinutes = Random.Shared.NextPositive();
        var factory = new JwtSecurityTokenAccessPassFactory(key, issuer, audience, expirationTimeInMinutes);
        var user = new User
        {
            Id = Random.Shared.NextPositive(),
            UserName = Guid.NewGuid().ToString(),
            Email = "someEmail@test.com"
        };
        var customClaim = new Claim("custom_claim_type", Guid.NewGuid().ToString());
        var claims = new List<Claim> { customClaim };
        string role1 = Guid.NewGuid().ToString();
        string role2 = Guid.NewGuid().ToString();
        var roles = new List<string> { role1, role2 };

        //Act
        var accessPass = factory.Create(user, claims, roles);

        //Assert
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(accessPass.Token);
        Assert.That(jwtToken.Issuer, Is.EqualTo(issuer));
        Assert.That(jwtToken.Audiences, Contains.Item(audience));
        Assert.That(jwtToken.Claims.Any(c => c.Type == customClaim.Type && c.Value == customClaim.Value), Is.True);
        Assert.That(jwtToken.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role1), Is.True);
        Assert.That(jwtToken.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role2), Is.True);
    }
}