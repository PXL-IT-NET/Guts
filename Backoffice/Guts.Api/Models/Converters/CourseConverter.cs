using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public class CourseConverter(IChapterConverter chapterConverter, IProjectConverter projectConverter)
        : ICourseConverter
    {
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
                Chapters = chapters.Select(chapter => chapterConverter.ToTopicModel(chapter)).ToList(),
                Projects = projects.Select(project => projectConverter.ToTopicModel(project)).ToList()
            };

            return model;
        }

        public CourseStatisticsModel ToCourseStatisticsModel(CourseStatisticsDto courseStatisticsDto)
        {
            ArgumentNullException.ThrowIfNull(courseStatisticsDto);

            var model = new CourseStatisticsModel
            {
                CourseId = courseStatisticsDto.CourseId,
                CourseCode = courseStatisticsDto.CourseCode,
                CourseName = courseStatisticsDto.CourseName,
                TotalAssignments = courseStatisticsDto.TotalAssignments,
                TotalTests = courseStatisticsDto.TotalTests,
                Students = courseStatisticsDto.Students.Select(s => new CourseStudentStatisticsModel
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
