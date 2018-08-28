using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Guts.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Guts.Business.Security
{
    public class JwtSecurityTokenAccessPassFactory : ITokenAccessPassFactory
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationTimeInMinutes;

        public JwtSecurityTokenAccessPassFactory(string key, string issuer, string audience, int expirationTimeInMinutes)
        {
            _key = key;
            _issuer = issuer;
            _audience = audience;
            _expirationTimeInMinutes = expirationTimeInMinutes;
        }

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

            var keyBytes = Encoding.UTF8.GetBytes(_key);
            var symmetricSecurityKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: allClaims,
                expires: DateTime.UtcNow.AddMinutes(_expirationTimeInMinutes),
                signingCredentials: signingCredentials);

            return new TokenAccessPass
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Expiration = jwtSecurityToken.ValidTo
            };
        }
    }
}