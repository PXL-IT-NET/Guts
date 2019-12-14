using System.Collections.Generic;
using System.Security.Claims;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Security
{
    public interface ITokenAccessPassFactory
    {
        TokenAccessPass Create(User user, IList<Claim> currentUserClaims, IList<string> userRoles);
    }
}
