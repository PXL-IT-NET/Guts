using Microsoft.AspNetCore.Identity;

namespace Guts.Domain
{
    public class User : IdentityUser<int>, IDomainObject
    {
    }
}
