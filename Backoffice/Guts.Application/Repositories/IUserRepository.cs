using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Domain.UserAggregate;

namespace Guts.Application.Repositories;

public record UserWithRolesAndLinks(
    User User,
    IReadOnlyList<string> Roles,
    UserCourseLinks CourseLinks);

public interface IUserRepository
{
    Task<User> GetByIdAsync(int userId);

    Task<IList<User>> GetUsersOfTopicAsync(int topicId);

    Task<IList<User>> GetUsersOfCourseForCurrentPeriodAsync(int courseId);

    Task<IReadOnlyList<string>> GetRolesAsync(User user);

    Task<IList<Claim>> GetClaimsAsync(User user);

    Task<UserCourseLinks> GetCourseLinksAsync(int userId);

    Task<IReadOnlyList<UserWithRolesAndLinks>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool OrphanedOnly,
        int maxResults = 50);

    Task<IReadOnlyList<UserWithRolesAndLinks>> GetOrphanedNonAdminUsersAsync();

    Task<int> CountAdminsAsync();

    Task AddToRoleAsync(User user, string roleName);

    Task RemoveFromRoleAsync(User user, string roleName);

    Task DeleteAsync(User user);
}
