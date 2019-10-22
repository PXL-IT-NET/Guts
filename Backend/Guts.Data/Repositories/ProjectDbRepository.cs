using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
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
            var query = from project in _context.Projects
                where project.CourseId == courseId 
                      && project.Code == projectCode 
                      && project.PeriodId == periodId
                select new Project(project.Id)
                {
                    Code = project.Code,
                    Description = project.Description,
                    Assignments = project.Assignments,
                    CourseId = project.CourseId,
                    PeriodId = project.PeriodId,
                    Teams = project.Teams.Where(t => t.TeamUsers.Any(tu => tu.UserId == userId)).ToList()
                };

            var result = await query.FirstOrDefaultAsync();
            if (result == null)
            {
                throw new DataNotFoundException();
            }
            return result;
        }
    }
}