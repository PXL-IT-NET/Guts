using Microsoft.AspNetCore.Identity;

namespace Guts.Domain
{
    public class Role : IdentityRole<int>, IDomainObject
    {
        public class Constants
        {
            public const string Student = "student";
            public const string Lector = "lector";
        }
    }
}
