using System.Collections.Generic;
using Guts.Business.Dtos;
using Guts.Domain.TopicAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ITopicConverter
    {
        TopicSummaryModel ToTopicSummaryModel(ITopic topic, IReadOnlyList<AssignmentResultDto> assignmentResults);
        TopicStatisticsModel ToTopicStatisticsModel(ITopic topic, IReadOnlyList<AssignmentStatisticsDto> assignmentStatistics, string unit);
    }
}