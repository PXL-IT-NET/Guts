using Microsoft.AspNetCore.Identity;

namespace Guts.Domain
{
    public class Role : IdentityRole<int>, IDomainObject
    {
    }
}
