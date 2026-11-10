using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TestAggregate;

namespace Guts.Application.Repositories;

public interface ITestRepository : IBasicRepository<Test>
{
    Task<IList<Test>> FindByAssignmentId(int assignmentId);
}