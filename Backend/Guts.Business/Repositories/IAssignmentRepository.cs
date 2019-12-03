using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Business.Repositories
{
    public interface IAssignmentRepository : IBasicRepository<Assignment>
    {
        Task<Assignment> GetSingleAsync(int topicId, string code);
        Task<Assignment> GetSingleWithTestsAsync(int assignmentId);
        Task<Assignment> GetSingleWithTestsAndCourseAsync(int assignmentId);
        Task<IList<Assignment>> GetByTopicWithTests(int topicId);
    }
}