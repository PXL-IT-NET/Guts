using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services
{
    public interface ITopicService
    {
        Task<ITopic> GetTopicAsync(string courseCode, string topicCode);

        Task<IReadOnlyList<ITopic>> GetTopicsByCourseWithAssignmentsAndTestsAsync(int courseId);
    }
}