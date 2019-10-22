using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;

namespace Guts.Business.Repositories
{
    public interface ICourseRepository: IBasicRepository<Course>
    {
        Task<Course> GetSingleAsync(string courseCode);
    }
}