using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;

namespace Guts.Application.Repositories;

public interface ICourseClassGroupRepository : IBasicRepository<CourseClassGroup>
{
    Task<IReadOnlyList<CourseClassGroup>> GetByCourseAndPeriodAsync(int courseId, int periodId);
    Task<CourseClassGroup> GetByNameAsync(int courseId, int periodId, string name);
}