using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Guts.Domain
{
    public class User : IdentityUser<int>, IDomainObject
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }
}
