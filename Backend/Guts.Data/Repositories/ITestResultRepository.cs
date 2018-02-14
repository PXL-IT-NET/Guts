using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestRun>
    {
        Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId);
    }
}