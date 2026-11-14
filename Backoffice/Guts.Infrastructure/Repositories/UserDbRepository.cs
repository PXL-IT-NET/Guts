using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class UserDbRepository : IUserRepository
{
    private readonly GutsContext _context;

    public UserDbRepository(GutsContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IList<User>> GetUsersOfTopicAsync(int topicId)
    {
        var userIds = await (from testrun in _context.TestRuns
            where testrun.Assignment.TopicId == topicId
            group testrun by testrun.UserId into g
            select g.Key).ToListAsync();

        var usersQuery = from user in _context.Users
            where userIds.Contains(user.Id)
            orderby user.FirstName, user.LastName
            select user;

        return await usersQuery.AsNoTracking().ToListAsync();
    }

    public async Task<IList<User>> GetUsersOfCourseForCurrentPeriodAsync(int courseId)
    {
        var today = DateTime.Today;
        var period = await _context.Periods.FirstOrDefaultAsync(p => p.From <= today && p.Until >= today);

        var userIds = await (from testrun in _context.TestRuns
            where testrun.Assignment.Topic.CourseId == courseId && testrun.Assignment.Topic.PeriodId == period.Id
            group testrun by testrun.UserId into g
            select g.Key).ToListAsync();

        var usersQuery = from user in _context.Users
            where userIds.Contains(user.Id)
            orderby user.FirstName, user.LastName
            select user;

        return await usersQuery.AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(User user)
    {
        return await (from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where ur.UserId == user.Id
            select r.Name).ToListAsync();
    }

    public async Task<IList<Claim>> GetClaimsAsync(User user)
    {
        var userClaims = await _context.UserClaims
            .Where(uc => uc.UserId == user.Id)
            .ToListAsync();

        return userClaims.Select(uc => new Claim(uc.ClaimType, uc.ClaimValue)).ToList();
    }

    public async Task<UserCourseLinks> GetCourseLinksAsync(int userId)
    {
        bool hasTestRuns = await _context.TestRuns.AnyAsync(tr => tr.UserId == userId);
        bool hasTestResults = await _context.TestResults.AnyAsync(tr => tr.UserId == userId);
        bool hasSolutionFiles = await _context.SolutionFiles.AnyAsync(sf => sf.UserId == userId);
        bool hasProjectTeams = await _context.ProjectTeamUsers.AnyAsync(ptu => ptu.UserId == userId);
        bool hasProfiles = await _context.StudentCourseProfiles.AnyAsync(sp => sp.UserId == userId);
        bool hasPeerAssessments = await _context.PeerAssessments
            .AnyAsync(pa => pa.User.Id == userId || pa.Subject.Id == userId);

        return new UserCourseLinks(
            hasTestRuns,
            hasTestResults,
            hasSolutionFiles,
            hasProjectTeams,
            hasProfiles,
            hasPeerAssessments);
    }

    public async Task<PagedResult<UserWithRolesAndLinks>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool orphanedOnly,
        int page = 1,
        int? pageSize = 50)
    {
        IQueryable<User> query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            string normalizedRole = roleFilter.Trim().ToLower();
            query = query.Where(u =>
                _context.UserRoles.Any(ur =>
                    ur.UserId == u.Id &&
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name.ToLower() == normalizedRole)));
        }

        if (orphanedOnly)
        {
            query = FilterOrphaned(query);
        }

        int totalCount = await query.CountAsync();
        int normalizedPage = Math.Max(1, page);
        int? normalizedPageSize = pageSize.HasValue ? Math.Max(1, pageSize.Value) : null;

        IQueryable<User> orderedQuery = query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id);

        if (normalizedPageSize.HasValue)
        {
            orderedQuery = orderedQuery
                .Skip((normalizedPage - 1) * normalizedPageSize.Value)
                .Take(normalizedPageSize.Value);
        }

        List<User> users = await orderedQuery.ToListAsync();
        IReadOnlyList<UserWithRolesAndLinks> items = await HydrateRolesAndLinksAsync(users);

        return new PagedResult<UserWithRolesAndLinks>
        {
            Items = items,
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };
    }

    public async Task<IReadOnlyList<UserWithRolesAndLinks>> GetWithRolesAndLinksByIdsAsync(IReadOnlyCollection<int> userIds)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return [];
        }

        List<int> distinctIds = userIds.Distinct().ToList();
        List<User> users = await _context.Users
            .Where(u => distinctIds.Contains(u.Id))
            .ToListAsync();

        return await HydrateRolesAndLinksAsync(users);
    }

    public async Task<int> CountAdminsAsync()
    {
        return await (from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where r.Name == Role.Constants.Admin
            select ur.UserId).Distinct().CountAsync();
    }

    public async Task AddToRoleAsync(User user, string roleName)
    {
        Role role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            return;
        }

        bool alreadyInRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (!alreadyInRole)
        {
            _context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<int>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFromRoleAsync(User user, string roleName)
    {
        Role role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            return;
        }

        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public Task DeleteAsync(User user)
    {
        return DeleteRangeAsync([user]);
    }

    public async Task DeleteRangeAsync(IReadOnlyCollection<User> users)
    {
        if (users == null || users.Count == 0)
        {
            return;
        }

        List<int> userIds = users.Select(u => u.Id).ToList();

        var userRoles = await _context.UserRoles.Where(ur => userIds.Contains(ur.UserId)).ToListAsync();
        _context.UserRoles.RemoveRange(userRoles);

        var userClaims = await _context.UserClaims.Where(uc => userIds.Contains(uc.UserId)).ToListAsync();
        _context.UserClaims.RemoveRange(userClaims);

        var userLogins = await _context.UserLogins.Where(ul => userIds.Contains(ul.UserId)).ToListAsync();
        _context.UserLogins.RemoveRange(userLogins);

        var userTokens = await _context.UserTokens.Where(ut => userIds.Contains(ut.UserId)).ToListAsync();
        _context.UserTokens.RemoveRange(userTokens);

        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync();
    }

    private IQueryable<User> FilterOrphaned(IQueryable<User> query)
    {
        return query.Where(u =>
            !_context.TestRuns.Any(tr => tr.UserId == u.Id) &&
            !_context.ProjectTeamUsers.Any(ptu => ptu.UserId == u.Id) &&
            !_context.StudentCourseProfiles.Any(sp => sp.UserId == u.Id) &&
            !_context.PeerAssessments.Any(pa => pa.User.Id == u.Id || pa.Subject.Id == u.Id));
    }

    private async Task<IReadOnlyList<UserWithRolesAndLinks>> HydrateRolesAndLinksAsync(IReadOnlyCollection<User> users)
    {
        if (users.Count == 0)
        {
            return [];
        }

        List<int> userIds = users.Select(u => u.Id).ToList();

        var rolePairs = await (from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }).ToListAsync();

        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());

        var testRunLinks = await (
            from tr in _context.TestRuns
            join a in _context.Assignments on tr.AssignmentId equals a.Id
            join t in _context.Topics on a.TopicId equals t.Id
            where userIds.Contains(tr.UserId)
            select new { tr.UserId, t.CourseId }).Distinct().ToListAsync();

        var testResultLinks = await (
            from tr in _context.TestResults
            join tst in _context.Tests on tr.TestId equals tst.Id
            join a in _context.Assignments on tst.AssignmentId equals a.Id
            join t in _context.Topics on a.TopicId equals t.Id
            where userIds.Contains(tr.UserId)
            select new { tr.UserId, t.CourseId }).Distinct().ToListAsync();

        var solutionFileLinks = await (
            from sf in _context.SolutionFiles
            join a in _context.Assignments on sf.AssignmentId equals a.Id
            join t in _context.Topics on a.TopicId equals t.Id
            where userIds.Contains(sf.UserId)
            select new { sf.UserId, t.CourseId }).Distinct().ToListAsync();

        var teamLinks = await (
            from ptu in _context.ProjectTeamUsers
            join pt in _context.ProjectTeams on ptu.ProjectTeamId equals pt.Id
            join t in _context.Topics on pt.ProjectId equals t.Id
            where userIds.Contains(ptu.UserId)
            select new { ptu.UserId, t.CourseId }).Distinct().ToListAsync();

        var profileLinks = await (
            from sp in _context.StudentCourseProfiles
            where userIds.Contains(sp.UserId)
            select new { sp.UserId, sp.CourseId }).Distinct().ToListAsync();

        var peerEvaluatorLinks = await (
            from pa in _context.PeerAssessments
            join pta in _context.ProjectTeamAssessments on pa.ProjectTeamAssessmentId equals pta.Id
            join pt in _context.ProjectTeams on EF.Property<int>(pta, "ProjectTeamId") equals pt.Id
            join t in _context.Topics on pt.ProjectId equals t.Id
            where userIds.Contains(pa.User.Id)
            select new { UserId = pa.User.Id, t.CourseId }).Distinct().ToListAsync();

        var peerSubjectLinks = await (
            from pa in _context.PeerAssessments
            join pta in _context.ProjectTeamAssessments on pa.ProjectTeamAssessmentId equals pta.Id
            join pt in _context.ProjectTeams on EF.Property<int>(pta, "ProjectTeamId") equals pt.Id
            join t in _context.Topics on pt.ProjectId equals t.Id
            where userIds.Contains(pa.Subject.Id)
            select new { UserId = pa.Subject.Id, t.CourseId }).Distinct().ToListAsync();

        HashSet<int> testRunUserIds = testRunLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> testResultUserIds = testResultLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> solutionFileUserIds = solutionFileLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> teamUserIds = teamLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> profileUserIds = profileLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> peerEvaluatorIds = peerEvaluatorLinks.Select(x => x.UserId).ToHashSet();
        HashSet<int> peerSubjectIds = peerSubjectLinks.Select(x => x.UserId).ToHashSet();

        var allUserCoursePairs = testRunLinks
            .Concat(testResultLinks)
            .Concat(solutionFileLinks)
            .Concat(teamLinks)
            .Concat(profileLinks)
            .Concat(peerEvaluatorLinks)
            .Concat(peerSubjectLinks)
            .Distinct()
            .ToList();

        List<int> distinctCourseIds = allUserCoursePairs.Select(x => x.CourseId).Distinct().ToList();
        Dictionary<int, LinkedCourseDto> coursesById = distinctCourseIds.Count == 0
            ? new Dictionary<int, LinkedCourseDto>()
            : await _context.Courses
                .Where(c => distinctCourseIds.Contains(c.Id))
                .Select(c => new LinkedCourseDto(c.Id, c.Code, c.Name))
                .ToDictionaryAsync(c => c.Id);

        ILookup<int, LinkedCourseDto> coursesByUser = allUserCoursePairs
            .Select(x => new { x.UserId, Course = coursesById.GetValueOrDefault(x.CourseId) })
            .Where(x => x.Course != null)
            .ToLookup(x => x.UserId, x => x.Course);

        return users.Select(u =>
        {
            IReadOnlyList<string> roles = rolesByUser.TryGetValue(u.Id, out var r) ? r : [];
            var links = new UserCourseLinks(
                testRunUserIds.Contains(u.Id),
                testResultUserIds.Contains(u.Id),
                solutionFileUserIds.Contains(u.Id),
                teamUserIds.Contains(u.Id),
                profileUserIds.Contains(u.Id),
                peerEvaluatorIds.Contains(u.Id) || peerSubjectIds.Contains(u.Id));
            IReadOnlyList<LinkedCourseDto> linkedCourses = coursesByUser[u.Id]
                .OrderBy(c => c.Code)
                .ThenBy(c => c.Name)
                .ToList();
            return new UserWithRolesAndLinks(u, roles, links, linkedCourses);
        }).ToList();
    }
}
