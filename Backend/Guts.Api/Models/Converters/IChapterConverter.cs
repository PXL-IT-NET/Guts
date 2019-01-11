using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        ChapterSummaryModel ToChapterSummaryModel(Chapter chapter, IList<AssignmentResultDto> userAssignmentResults);
        ChapterModel ToChapterModel(Chapter chapter);
        ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers);
        ChapterStatisticsModel ToChapterStatisticsModel(Chapter chapter, IList<AssignmentStatisticsDto> chapterStatistics);
    }
}
