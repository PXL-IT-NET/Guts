using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services
{
    internal class TopicService(
        ITopicRepository topicRepository,
        IPeriodRepository periodRepository)
        : ITopicService
    {
        public async Task<ITopic> GetTopicAsync(string courseCode, string topicCode, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);
            return await topicRepository.GetSingleAsync(courseCode, topicCode, period.Id);
        }

        public async Task<IReadOnlyList<ITopic>> GetTopicsByCourseWithAssignmentsAndTestsAsync(int courseId, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);
            return await topicRepository.GetByCourseWithAssignmentsAndTestsAsync(courseId, period.Id);
        }
    }
}