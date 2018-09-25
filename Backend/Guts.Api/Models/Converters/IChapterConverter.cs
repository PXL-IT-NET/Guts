using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        ChapterSummaryModel ToChapterSummaryModel(Chapter chapter, 
            IList<ExerciseResultDto> userExerciseResults,
            IList<ExerciseResultDto> averageExerciseResults);
        ChapterModel ToChapterModel(Chapter chapter);
        ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers);
    }
}
