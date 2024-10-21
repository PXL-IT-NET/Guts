using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.TopicAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class TopicDbRepository : BaseDbRepository<ITopic, Topic>, ITopicRepository
    {
        public TopicDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<ITopic> GetSingleAsync(string courseCode, string code, int periodId)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Course.Code == courseCode && t.Code == code && t.PeriodId == periodId);
            if (topic == null)
            {
                throw new DataNotFoundException();
            }
            return topic;
        }

        public async Task UpdateAsync(int courseId, string code, int periodId, string description)
        {
            Topic topic = await _context.Topics.FirstOrDefaultAsync(t => t.Course.Id == courseId && t.Code == code && t.PeriodId == periodId);
            Contracts.Require(topic is not null, $"Project or chapter with code '{code}' could not be found");

            topic!.Description = description;
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ITopic>> GetByCourseWithAssignmentsAndTestsAsync(int courseId, int periodId)
        {
            var query = _context.Topics.Where(t => t.CourseId == courseId && t.PeriodId == periodId).Include(t => t.Assignments).ThenInclude(a => a.Tests);
            return await query.ToListAsync();
        }
    }
}