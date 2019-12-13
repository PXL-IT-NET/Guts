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
            var query = from testrun in _context.TestRuns
                        where testrun.Assignment.TopicId == topicId
                        select testrun.User;
           
            return await query
                .Distinct()
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .AsNoTracking().ToListAsync();
        }

        public async Task<IList<User>> GetUsersOfCourseForCurrentPeriodAsync(int courseId)
        {
            var today = DateTime.Today;
            var period = await _context.Periods.FirstOrDefaultAsync(p => p.From <= today && p.Until >= today);

            var query = from testrun in _context.TestRuns
                        where testrun.Assignment.Topic.CourseId == courseId && testrun.Assignment.Topic.PeriodId == period.Id
                        select testrun.User;
           
            return await query
                .Distinct()
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .AsNoTracking().ToListAsync();
        }
    }
}