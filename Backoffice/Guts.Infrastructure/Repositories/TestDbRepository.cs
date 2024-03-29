using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class TestDbRepository : BaseDbRepository<Test, Test>, ITestRepository
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