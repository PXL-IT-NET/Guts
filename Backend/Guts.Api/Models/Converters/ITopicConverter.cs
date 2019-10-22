using System.Collections.Generic;
using Guts.Business.Dtos;
using Guts.Domain.TopicAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ITopicConverter
    {
        TopicSummaryModel ToTopicSummaryModel(Topic topic, IList<AssignmentResultDto> assignmentResults);
        TopicStatisticsModel ToTopicStatisticsModel(Topic topic, IList<AssignmentStatisticsDto> assignmentStatistics, string unit);
    }
}