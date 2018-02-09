using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ICourseRepository
    {
        Task<Course> GetSingleAsync(string courseCode);
    }
}