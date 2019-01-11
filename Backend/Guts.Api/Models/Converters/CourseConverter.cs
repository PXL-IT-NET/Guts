using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class CourseConverter : ICourseConverter
    {
        private readonly IChapterConverter _chapterConverter;
        private readonly IProjectConverter _projectConverter;

        public CourseConverter(IChapterConverter chapterConverter, IProjectConverter projectConverter)
        {
            _chapterConverter = chapterConverter;
            _projectConverter = projectConverter;
        }

        public CourseContentsModel ToCourseContentsModel(Course course, 
            IList<Chapter> chapters, 
            IList<Project> projects)
        {
            if (course == null || chapters == null || projects == null)
            {
                throw new ArgumentException("None of the arguments 'course' or 'chapters' can be null.");
            }

            var model = new CourseContentsModel
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Chapters = chapters.Select(chapter => _chapterConverter.ToChapterModel(chapter)).ToList(),
                Projects = projects.Select(project => _projectConverter.ToProjectModel(project)).ToList()
            };

            return model;
        }
    }
}