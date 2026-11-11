using System;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Domain;

public abstract class Entity : IEntity, IEquatable<Entity>
{
    protected abstract IEnumerable<object> GetIdComponents();

    public virtual object[] IdentityValues => GetIdComponents()?.ToArray() ?? Array.Empty<object>();

    public virtual bool IsTransient()
    {
        var values = IdentityValues;
        if (values.Length == 0)
            return true;

        return values.Any(c => c == null || (c is int i && i == 0) || (c is long l && l == 0));
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Entity);
    }

    public virtual bool Equals(Entity other)
    {
        if (ReferenceEquals(other, null))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient() || other.IsTransient())
            return false;

        return IdentityValues.SequenceEqual(other.IdentityValues);
    }

    public static bool operator ==(Entity a, Entity b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            return true;

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity a, Entity b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        return IdentityValues
            .Aggregate(GetType().GetHashCode(), (current, obj) =>
            {
                unchecked
                {
                    return current * 23 + (obj?.GetHashCode() ?? 0);
                }
            });
    }
}
