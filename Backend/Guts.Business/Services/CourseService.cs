using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;

        public CourseService(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public async Task<IList<Course>> GetAllCoursesAsync()
        {
            return await _courseRepository.GetAllAsync();
        }
    }
}