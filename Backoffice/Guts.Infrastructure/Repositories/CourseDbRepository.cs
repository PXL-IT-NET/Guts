using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.CourseAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class CourseDbRepository : BaseDbRepository<Course, Course>, ICourseRepository
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

        public override async Task<IList<Course>> GetAllAsync()
        {
            return await _context.Set<Course>().OrderBy(c => c.Name).ToListAsync();
        }
    }
}