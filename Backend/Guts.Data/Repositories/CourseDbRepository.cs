using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class CourseDbRepository : ICourseRepository
    {
        private readonly GutsContext _context;

        public CourseDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<Course> GetSingleAsync(string courseCode)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code.ToLower() == courseCode.ToLower());
            if (course == null)
            {
                throw new DataNotFoundException();
            }
            return course;
        }
    }
}