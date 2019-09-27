using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface ITopicService
    {
        Task<Topic> GetTopicAsync(string courseCode, string topicCode);
    }
}