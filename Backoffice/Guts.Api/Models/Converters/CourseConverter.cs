using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

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
            IReadOnlyList<Chapter> chapters,
            IReadOnlyList<IProject> projects)
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
                Chapters = chapters.Select(chapter => _chapterConverter.ToTopicModel(chapter)).ToList(),
                Projects = projects.Select(project => _projectConverter.ToTopicModel(project)).ToList()
            };

            return model;
        }

        public CourseStatisticsModel ToCourseStatisticsModel(CourseStatisticsDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var model = new CourseStatisticsModel
            {
                CourseId = dto.CourseId,
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                TotalAssignments = dto.TotalAssignments,
                TotalTests = dto.TotalTests,
                Students = dto.Students.Select(s => new CourseStudentStatisticsModel
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    AssignmentsWorkedOn = s.AssignmentsWorkedOn,
                    TotalPassedTests = s.TotalPassedTests,
                    TotalTests = s.TotalTests,
                    CompletionPercentage = s.CompletionPercentage,
                    Chapters = s.Chapters.Select(c => new CourseStudentChapterStatisticsModel
                    {
                        ChapterId = c.ChapterId,
                        Code = c.Code,
                        Description = c.Description,
                        Assignments = c.Assignments.Select(a => new CourseStudentAssignmentStatisticsModel
                        {
                            AssignmentId = a.AssignmentId,
                            Code = a.Code,
                            Description = a.Description,
                            NumberOfPassedTests = a.NumberOfPassedTests,
                            NumberOfTests = a.NumberOfTests
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return model;
        }
    }
}
