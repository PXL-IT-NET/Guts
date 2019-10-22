using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Models.Converters
{
    public interface IChapterConverter
    {
        TopicModel ToTopicModel(Chapter chapter);
        ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers);
    }
}
