using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Guts.Api.Models.AssignmentModels;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        private readonly IUserConverter _userConverter;
        private readonly IMapper _mapper;

        public ChapterConverter(IUserConverter userConverter, IMapper mapper)
        {
            _userConverter = userConverter;
            _mapper = mapper;
        }

        public TopicModel ToTopicModel(Chapter chapter)
        {
            return new TopicModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Description = chapter.Description,
                Assignments = chapter.Assignments.Select(a => _mapper.Map<AssignmentModel>(a)).OrderBy(m => m.Code)
                    .ToList()
            };
        }

        public ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers)
        {
            return new ChapterDetailModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Description = chapter.Description,
                Exercises = chapter.Assignments.Select(assignment => new AssignmentModel
                {
                    AssignmentId = assignment.Id,
                    Code = assignment.Code,
                    Description = assignment.Description,
                    Tests = assignment.Tests.Select(t => new TestModel { Id = t.Id, TestName = t.TestName }).ToList()
                }).OrderBy(model => model.Description).ToList(),
                Users = chapterUsers.Select(user => _userConverter.FromUser(user)).ToList()
            };
        }
    }
}
