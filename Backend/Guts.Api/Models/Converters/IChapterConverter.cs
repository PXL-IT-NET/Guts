using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        ChapterContentsModel ToChapterContentsModel(Chapter chapter, IList<ExerciseResultDto> exerciseResults);
    }
}
