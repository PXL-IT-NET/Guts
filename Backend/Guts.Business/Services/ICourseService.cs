using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface ICourseService
    {
        Task<IList<Course>> GetAllCoursesAsync();
    }
}