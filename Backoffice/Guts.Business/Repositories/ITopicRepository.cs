using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Repositories
{
    public interface ITopicRepository : IBasicRepository<ITopic>
    {
        Task<ITopic> GetSingleAsync(string courseCode, string code, int periodId);

        Task<IReadOnlyList<ITopic>> GetByCourseWithAssignmentsAndTestsAsync(int courseId, int periodId);

        Task UpdateAsync(int courseId, string code, int periodId, string description);
    }
}