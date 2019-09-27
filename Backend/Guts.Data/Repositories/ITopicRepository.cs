using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITopicRepository : IBasicRepository<Topic>
    {
        Task<Topic> GetSingleAsync(string courseCode, string code, int periodId);
    }
}