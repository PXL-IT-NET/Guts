using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class TestDbRepository : BaseDbRepository<Test>, ITestRepository
    {
        public TestDbRepository(GutsContext context): base(context)
        {
        }

        public async Task<IList<Test>> FindByAssignmentId(int assignmentId)
        {
            return await _context.Tests.Where(t => t.AssignmentId == assignmentId).ToListAsync();
        }
    }
}