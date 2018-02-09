using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestRepository : IBasicRepository<Test>
    {
        Task<IList<Test>> FindByExercise(int exerciseId);
    }
}