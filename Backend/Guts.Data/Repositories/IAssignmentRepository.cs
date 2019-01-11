using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IAssignmentRepository : IBasicRepository<Assignment>
    {
        Task<Assignment> GetSingleAsync(int topicId, string code);
        Task<Assignment> GetSingleWithTestsAndCourseAsync(int assignmentId);
    }
}