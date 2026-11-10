using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Domain.CourseAggregate;

namespace Guts.Application.Services;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseByIdAsync(int courseId);
    Task<CourseStatisticsDto> GetCourseStatisticsAsync(int courseId, int? periodId = null);
}