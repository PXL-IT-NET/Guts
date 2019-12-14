using Microsoft.AspNetCore.Identity;

namespace Guts.Domain.RoleAggregate
{
    public class Role : IdentityRole<int>, IEntity
    {
        public class Constants
        {
            public const string Student = "student";
            public const string Lector = "lector";
        }

        public override bool Equals(object obj)
        {
            var other = obj as Role;

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

        public static bool operator ==(Role a, Role b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Role a, Role b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (GetType().ToString() + Id).GetHashCode();
        }
    }
}
