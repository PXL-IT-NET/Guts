namespace Guts.Domain
{
    public abstract class AggregateRoot : Entity
    {
        protected AggregateRoot()
        {
        }

        protected AggregateRoot(int id) : base(id) { }


        //logic to add domain events can be added here
    }
}