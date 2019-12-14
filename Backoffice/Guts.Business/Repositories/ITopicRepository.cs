using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Repositories
{
    public interface ITopicRepository : IBasicRepository<Topic>
    {
        Task<Topic> GetSingleAsync(string courseCode, string code, int periodId);

        Task<IList<Topic>> GetByCourseWithAssignmentsAndTestsAsync(int courseId, int periodId);
    }
}