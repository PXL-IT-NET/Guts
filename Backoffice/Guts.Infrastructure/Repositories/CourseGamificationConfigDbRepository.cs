using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class CourseGamificationConfigDbRepository : BaseDbRepository<CourseGamificationConfig, CourseGamificationConfig>, ICourseGamificationConfigRepository
{
    public CourseGamificationConfigDbRepository(GutsContext context) : base(context) { }

    public async Task<CourseGamificationConfig> GetByCourseIdAsync(int courseId)
    {
        return await _context.CourseGamificationConfigs
            .FirstOrDefaultAsync(cgc => cgc.CourseId == courseId);
    }
}