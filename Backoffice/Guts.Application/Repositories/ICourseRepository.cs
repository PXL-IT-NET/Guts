using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;

namespace Guts.Application.Repositories;

public interface ICourseRepository: IBasicRepository<Course>
{
    Task<Course> GetSingleAsync(string courseCode);
}