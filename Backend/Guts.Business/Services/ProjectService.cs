using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;

        public ProjectService(IProjectRepository projectRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository)
        {
            _projectRepository = projectRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
        }

        public async Task<Project> GetProjectAsync(string courseCode, string projectCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _projectRepository.GetSingleAsync(courseCode, projectCode, currentPeriod.Id);
        }

        public async Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            Project project;
            try
            {
                project = await _projectRepository.GetSingleAsync(courseCode, projectCode, currentPeriod.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await _courseRepository.GetSingleAsync(courseCode);
                project = new Project
                {
                    Code = projectCode,
                    CourseId = course.Id,
                    PeriodId = currentPeriod.Id,
                    Description = string.Empty
                };
                project = await _projectRepository.AddAsync(project);
            }

            return project;
        }

        public async Task<IList<Project>> GetProjectsOfCourseAsync(int courseId)
        {
            try
            {
                var period = await _periodRepository.GetCurrentPeriodAsync();
                var projects = await _projectRepository.GetByCourseIdAsync(courseId, period.Id);
                return projects.OrderBy(p => p.Description).ToList();
            }
            catch (DataNotFoundException)
            {
                return new List<Project>();
            }
        }
    }
}