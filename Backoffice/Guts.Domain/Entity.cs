using Guts.Common;

namespace Guts.Domain
{
    public abstract class Entity : IEntity
    {
        private int _id;

        public virtual int Id
        {
            get => _id; 
            set //TODO: make this a protected setter
            {
                Contracts.Require(value >= 0, "Id cannot be negative");
                _id = value;
            }
        }

        protected Entity()
        {
            _id = 0;
        }

        protected Entity(int id)
        {
            Contracts.Require(id > 0, "Id must be positive for existing entities.");
            _id = id;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Entity;

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
            return (GetType().ToString() + Id).GetHashCode();
        }
    }
}