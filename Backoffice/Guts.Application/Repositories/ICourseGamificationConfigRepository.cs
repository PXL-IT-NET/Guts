using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Configuration;

namespace Guts.Application.Repositories;

public interface ICourseGamificationConfigRepository : IBasicRepository<CourseGamificationConfig>
{
    Task<CourseGamificationConfig> GetByCourseIdAsync(int courseId);
}