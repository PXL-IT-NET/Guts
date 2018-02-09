using Guts.Domain;

namespace Guts.Data.Repositories
{
    public class TestRunDbRepository : BaseDbRepository<TestRun>, ITestRunRepository
    {
        public TestRunDbRepository(GutsContext context) : base(context)
        {
        }
    }
}