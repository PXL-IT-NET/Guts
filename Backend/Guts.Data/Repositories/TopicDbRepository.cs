using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
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
    }
}