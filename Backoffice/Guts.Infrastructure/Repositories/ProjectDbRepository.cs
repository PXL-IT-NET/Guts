using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    public class ProjectDbRepository : BaseDbRepository<Project>, IProjectRepository
    {
        public ProjectDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Project> GetSingleAsync(string courseCode, string projectCode, int periodId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Course.Code == courseCode && p.Code == projectCode && p.PeriodId == periodId);
            if (project == null)
            {
                throw new DataNotFoundException();
            }
            return project;
        }

        public async Task<Project> GetSingleAsync(int courseId, string projectCode, int periodId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.CourseId == courseId && p.Code == projectCode && p.PeriodId == periodId);
            if (project == null)
            {
                throw new DataNotFoundException();
            }
            return project;
        }

        public async Task<IList<Project>> GetByCourseIdAsync(int courseId, int periodId)
        {
            var query = _context.Projects.Where(p => p.CourseId == courseId && p.PeriodId == periodId);

            return await query.ToListAsync();
        }

        public async Task<Project> LoadWithAssignmentsAndTeamsAsync(int courseId, string projectCode, int periodId)
        {
            var query = _context.Projects.Where(p => p.CourseId == courseId && p.PeriodId == periodId);
            query = query.Include(p => p.Assignments).Include(p => p.Teams);

            var project = await query.FirstOrDefaultAsync();
            if (project == null)
            {
                throw new DataNotFoundException();
            }
            return project;
        }

        public async Task<Project> LoadWithAssignmentsAndTeamsOfUserAsync(int courseId, string projectCode, int periodId, int userId)
        {
            var query = _context.Projects.Where(p => p.CourseId == courseId && p.PeriodId == periodId);
            query = query.Include(p => p.Assignments).Include(p => p.Teams);

            var project = await query.FirstOrDefaultAsync();
            if (project == null)
            {
                throw new DataNotFoundException();
            }

            //only return the teams of the user
            var userProjectTeamIds = await _context.ProjectTeamUsers.Where(ptu => ptu.UserId == userId)
                .Select(ptu => ptu.ProjectTeamId).ToListAsync();
            project.Teams = project.Teams.Where(pt => userProjectTeamIds.Any(teamId => teamId == pt.Id)).ToList();

            return project;
        }
    }
}