using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.AspNetCore.Identity;

namespace Guts.Domain.RoleAggregate
{
    [SuppressMessage("Major Code Smell", "S4035:Classes implementing 'IEquatable<T>' should be sealed or also implement 'IEqualityComparer<T>'", Justification = "Equals implementation explicitly checks GetType() to avoid derived type equality.")]
    public class Role : IdentityRole<int>, IEntity, IEquatable<Role>
    {
        public static class Constants
        {
            public const string Student = "student";
            public const string Lector = "lector";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Role);
        }

        public bool Equals(Role other)
        {
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
