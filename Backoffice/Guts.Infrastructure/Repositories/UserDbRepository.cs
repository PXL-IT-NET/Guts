using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Application.Repositories;
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

    public async Task<IReadOnlyList<UserWithRolesAndLinks>> SearchUsersAsync(
        string search,
        string roleFilter,
        bool OrphanedOnly,
        int maxResults = 50)
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

        if (OrphanedOnly)
        {
            query = FilterOrphaned(query);
        }

        List<User> users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(maxResults)
            .ToListAsync();

        return await HydrateRolesAndLinksAsync(users);
    }

    public async Task<IReadOnlyList<UserWithRolesAndLinks>> GetOrphanedNonAdminUsersAsync()
    {
        IQueryable<User> query = FilterOrphaned(_context.Users)
            .Where(u => !_context.UserRoles.Any(ur =>
                ur.UserId == u.Id &&
                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == Role.Constants.Admin)));

        List<User> users = await query.ToListAsync();
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

    public async Task DeleteAsync(User user)
    {
        var userRoles = await _context.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
        _context.UserRoles.RemoveRange(userRoles);

        var userClaims = await _context.UserClaims.Where(uc => uc.UserId == user.Id).ToListAsync();
        _context.UserClaims.RemoveRange(userClaims);

        var userLogins = await _context.UserLogins.Where(ul => ul.UserId == user.Id).ToListAsync();
        _context.UserLogins.RemoveRange(userLogins);

        var userTokens = await _context.UserTokens.Where(ut => ut.UserId == user.Id).ToListAsync();
        _context.UserTokens.RemoveRange(userTokens);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    private IQueryable<User> FilterOrphaned(IQueryable<User> query)
    {
        return query.Where(u =>
            !_context.TestRuns.Any(tr => tr.UserId == u.Id) &&
            !_context.TestResults.Any(tr => tr.UserId == u.Id) &&
            !_context.SolutionFiles.Any(sf => sf.UserId == u.Id) &&
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

        var testRunUserIds = await _context.TestRuns.Where(tr => userIds.Contains(tr.UserId)).Select(tr => tr.UserId).Distinct().ToListAsync();
        var testResultUserIds = await _context.TestResults.Where(tr => userIds.Contains(tr.UserId)).Select(tr => tr.UserId).Distinct().ToListAsync();
        var solutionFileUserIds = await _context.SolutionFiles.Where(sf => userIds.Contains(sf.UserId)).Select(sf => sf.UserId).Distinct().ToListAsync();
        var teamUserIds = await _context.ProjectTeamUsers.Where(ptu => userIds.Contains(ptu.UserId)).Select(ptu => ptu.UserId).Distinct().ToListAsync();
        var profileUserIds = await _context.StudentCourseProfiles.Where(sp => userIds.Contains(sp.UserId)).Select(sp => sp.UserId).Distinct().ToListAsync();
        var peerEvaluatorIds = await _context.PeerAssessments.Where(pa => userIds.Contains(pa.User.Id)).Select(pa => pa.User.Id).Distinct().ToListAsync();
        var peerSubjectIds = await _context.PeerAssessments.Where(pa => userIds.Contains(pa.Subject.Id)).Select(pa => pa.Subject.Id).Distinct().ToListAsync();

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
            return new UserWithRolesAndLinks(u, roles, links);
        }).ToList();
    }
}
