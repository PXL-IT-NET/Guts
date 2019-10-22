using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;

namespace Guts.Business.Services
{
    public interface ICourseService
    {
        Task<IList<Course>> GetAllCoursesAsync();
        Task<Course> GetCourseByIdAsync(int courseId);
    }
}