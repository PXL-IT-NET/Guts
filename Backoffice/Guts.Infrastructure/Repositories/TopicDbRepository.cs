using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    public class TopicDbRepository : BaseDbRepository<Topic>, ITopicRepository
    {
        public TopicDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Topic> GetSingleAsync(string courseCode, string code, int periodId)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Course.Code == courseCode && t.Code == code && t.PeriodId == periodId);
            if (topic == null)
            {
                throw new DataNotFoundException();
            }
            return topic;
        }

        public async Task<IList<Topic>> GetByCourseWithAssignmentsAndTestsAsync(int courseId, int periodId)
        {
            var query = _context.Topics.Where(t => t.CourseId == courseId && t.PeriodId == periodId).Include(t => t.Assignments).ThenInclude(a => a.Tests);
            return await query.ToListAsync();
        }
    }
}