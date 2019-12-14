using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TestAggregate;

namespace Guts.Business.Repositories
{
    public interface ITestRepository : IBasicRepository<Test>
    {
        Task<IList<Test>> FindByAssignmentId(int assignmentId);
    }
}