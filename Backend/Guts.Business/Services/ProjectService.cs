using System.Threading.Tasks;
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

        public Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode)
        {
            throw new System.NotImplementedException();
        }
    }
}