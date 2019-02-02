using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        TopicModel ToTopicModel(Chapter chapter);
        ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers);
    }
}
