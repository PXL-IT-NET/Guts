using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Domain.CourseAggregate;

namespace Guts.Application.Services;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseByIdAsync(int courseId);
    Task<Course> CreateCourseAsync(string code, string name);
    Task<Course> UpdateCourseAsync(int courseId, string code, string name);
    Task DeleteCourseAsync(int courseId);
    Task<CourseStatisticsDto> GetCourseStatisticsAsync(int courseId, int? periodId = null);
}