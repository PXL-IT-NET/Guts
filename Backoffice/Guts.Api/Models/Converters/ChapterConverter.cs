using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.AssignmentModels;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        private readonly IUserConverter _userConverter;

        public ChapterConverter(IUserConverter userConverter)
        {
            _userConverter = userConverter;
        }

        public TopicModel ToTopicModel(Chapter chapter)
        {
            return new TopicModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Description = chapter.Description
            };
        }

        public ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers)
        {
            return new ChapterDetailModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Exercises = chapter.Assignments.Select(assignment => new AssignmentModel
                {
                    AssignmentId = assignment.Id,
                    Code = assignment.Code,
                    Description = assignment.Description
                }).OrderBy(model => model.Description).ToList(),
                Users = chapterUsers.Select(user => _userConverter.FromUser(user)).ToList()
            };
        }
    }
}
