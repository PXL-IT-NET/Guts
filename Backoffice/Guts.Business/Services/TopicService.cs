using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services
{
    internal class TopicService : ITopicService
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IPeriodRepository _periodRepository;

        public TopicService(ITopicRepository topicRepository,
            IPeriodRepository periodRepository)
        {
            _topicRepository = topicRepository;
            _periodRepository = periodRepository;
        }
        public async Task<ITopic> GetTopicAsync(string courseCode, string topicCode, int? periodId = null)
        {
            Period period = await _periodRepository.GetPeriodAsync(periodId);
            return await _topicRepository.GetSingleAsync(courseCode, topicCode, period.Id);
        }

        public async Task<IReadOnlyList<ITopic>> GetTopicsByCourseWithAssignmentsAndTestsAsync(int courseId, int? periodId = null)
        {
            Period period = await _periodRepository.GetPeriodAsync(periodId);
            return await _topicRepository.GetByCourseWithAssignmentsAndTestsAsync(courseId, period.Id);
        }
    }
}