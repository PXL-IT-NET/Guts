using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Repositories;

public interface IUserRepository
{
    Task<User> GetByIdAsync(int userId);

    Task<IList<User>> GetUsersOfTopicAsync(int topicId);

    Task<IList<User>> GetUsersOfCourseForCurrentPeriodAsync(int courseId);
}
