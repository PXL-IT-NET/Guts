using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.ProjectTeamAggregate;
using Microsoft.AspNetCore.Identity;

namespace Guts.Domain.UserAggregate;

[SuppressMessage("Major Code Smell", "S4035:Classes implementing 'IEquatable<T>' should be sealed or also implement 'IEqualityComparer<T>'", Justification = "Equals implementation explicitly checks GetType() to avoid derived type equality.")]
public class User : IdentityUser<int>, IEntity, IEquatable<User>
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; } = new HashSet<ProjectTeamUser>();

    public override bool Equals(object obj)
    {
        return Equals(obj as User);
    }

    public bool Equals(User other)
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