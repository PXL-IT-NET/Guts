using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services
{
    public class TopicService : ITopicService
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IPeriodRepository _periodRepository;

        public TopicService(ITopicRepository topicRepository,
            IPeriodRepository periodRepository)
        {
            _topicRepository = topicRepository;
            _periodRepository = periodRepository;
        }
        public async Task<Topic> GetTopicAsync(string courseCode, string topicCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _topicRepository.GetSingleAsync(courseCode, topicCode, currentPeriod.Id);
        }

        public async Task<IList<Topic>> GetTopicsByCourseWithAssignmentsAndTestsAsync(int courseId)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            return await _topicRepository.GetByCourseWithAssignmentsAndTestsAsync(courseId, currentPeriod.Id);
        }
    }
}