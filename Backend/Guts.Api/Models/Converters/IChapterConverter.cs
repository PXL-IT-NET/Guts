using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        TopicSummaryModel ToTopicSummaryModel(Chapter chapter, IList<AssignmentResultDto> userAssignmentResults);
        TopicModel ToChapterModel(Chapter chapter);
        ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers);
        TopicStatisticsModel ToTopicStatisticsModel(Chapter chapter, IList<AssignmentStatisticsDto> chapterStatistics);
    }
}
