using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class CourseDbRepository : BaseDbRepository<Course>, ICourseRepository
    {
        public CourseDbRepository(GutsContext context) : base(context)
        {
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