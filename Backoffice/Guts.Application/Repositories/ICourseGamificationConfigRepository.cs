using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;

namespace Guts.Application.Repositories;

public interface ICourseGamificationConfigRepository : IBasicRepository<CourseGamificationConfig>
{
    Task<CourseGamificationConfig> GetByCourseIdAsync(int courseId);
}