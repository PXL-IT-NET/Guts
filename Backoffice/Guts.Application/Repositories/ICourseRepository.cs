using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Repositories;

public interface ICourseRepository: IBasicRepository<Course>
{
    Task<Course> GetSingleAsync(string courseCode);
    Task QueueDeletionAsync(Course course, DeletionJob job);
}