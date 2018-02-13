using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ICourseRepository: IBasicRepository<Course>
    {
        Task<Course> GetSingleAsync(string courseCode);
    }
}