using System.Collections.Generic;
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

        public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; }
    }
}
