using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.UserAggregate;
using System;

namespace Guts.Data.Repositories
{
    public class UserDbRepository : IUserRepository
    {
        private readonly GutsContext _context;

        public UserDbRepository(GutsContext context)
        {
            _context = context;
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
    }
}