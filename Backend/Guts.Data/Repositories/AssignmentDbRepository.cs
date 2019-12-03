using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class AssignmentDbRepository : BaseDbRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Assignment> GetSingleAsync(int topicId, string code)
        {
            var assignment = await _context.Assignments
                .Where(a => a.TopicId == topicId && a.Code == code)
                .Include(a => a.TestCodeHashes)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                throw new DataNotFoundException();
            }
            return assignment;
        }

        public async Task<Assignment> GetSingleWithTestsAsync(int assignmentId)
        {
            var assignment = await _context.Assignments.Where(a => a.Id == assignmentId)
                .Include(a => a.Tests)
                .FirstOrDefaultAsync();
            if (assignment == null)
            {
                throw new DataNotFoundException();
            }
            return assignment;
        }

        public async Task<Assignment> GetSingleWithTestsAndCourseAsync(int assignmentId)
        {
            var assignment = await _context.Assignments.Where(a => a.Id == assignmentId)
                .Include(a => a.Topic.Course)
                .Include(a => a.Tests)
                .FirstOrDefaultAsync();
            if (assignment == null)
            {
                throw new DataNotFoundException();
            }
            return assignment;
        }

        public async Task<IList<Assignment>> GetByTopicWithTests(int topicId)
        {
            var assignments = await _context.Assignments.Where(a => a.TopicId == topicId).Include(a => a.Tests)
                .ToListAsync();
            return assignments;
        }
    }
}