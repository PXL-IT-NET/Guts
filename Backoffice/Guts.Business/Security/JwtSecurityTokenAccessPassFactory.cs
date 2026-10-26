using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Guts.Domain.UserAggregate;
using Microsoft.IdentityModel.Tokens;

namespace Guts.Business.Security
{
    internal class JwtSecurityTokenAccessPassFactory(
        string key,
        string issuer,
        string audience,
        int expirationTimeInMinutes)
        : ITokenAccessPassFactory
    {
        public TokenAccessPass Create(User user, IList<Claim> currentUserClaims, IList<string> userRoles)
        {
            var allClaims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            }.Union(currentUserClaims).ToList();

            foreach (var role in userRoles)
            {
                var roleClaim = new Claim(ClaimTypes.Role, role);
                allClaims.Add(roleClaim);
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var symmetricSecurityKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: allClaims,
                expires: DateTime.UtcNow.AddMinutes(expirationTimeInMinutes),
                signingCredentials: signingCredentials);

            return new TokenAccessPass
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Expiration = jwtSecurityToken.ValidTo
            };
        }
    }
}