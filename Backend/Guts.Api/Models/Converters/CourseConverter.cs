using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class CourseConverter : ICourseConverter
    {
        private readonly IChapterConverter _chapterConverter;

        public CourseConverter(IChapterConverter chapterConverter)
        {
            _chapterConverter = chapterConverter;
        }

        public CourseContentsModel ToCourseContentsModel(Course course, IList<Chapter> chapters)
        {
            if (course == null || chapters == null)
            {
                throw new ArgumentException("None of the arguments 'course' or 'chapters' can be null.");
            }

            var model = new CourseContentsModel
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Chapters = chapters.Select(chapter => _chapterConverter.ToChapterModel(chapter)).ToList()
            };

            return model;
        }
    }
}