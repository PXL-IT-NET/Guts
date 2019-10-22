using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.ProjectTeamAggregate;
using Microsoft.AspNetCore.Identity;

namespace Guts.Domain.UserAggregate
{
    public class User : IdentityUser<int>, IEntity
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; } = new HashSet<ProjectTeamUser>();

        public override bool Equals(object obj)
        {
            var other = obj as User;

            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (Id == 0 || other.Id == 0)
                return false;

            return Id == other.Id;
        }

        public static bool operator ==(User a, User b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(User a, User b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (GetType().ToString() + Id).GetHashCode();
        }
    }
}
