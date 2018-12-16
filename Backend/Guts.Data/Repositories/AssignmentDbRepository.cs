using Guts.Domain;

namespace Guts.Data.Repositories
{
    public class AssignmentDbRepository : BaseDbRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentDbRepository(GutsContext context) : base(context)
        {
        }
    }
}