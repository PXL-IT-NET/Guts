using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.CourseAggregate;

namespace Guts.Business.Services
{
    internal class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly ITestResultRepository _testResultRepository;

        public CourseService(
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository,
            IChapterRepository chapterRepository,
            ITestResultRepository testResultRepository)
        {
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _chapterRepository = chapterRepository;
            _testResultRepository = testResultRepository;
        }

        public async Task<IReadOnlyList<Course>> GetAllCoursesAsync()
        {
            return await _courseRepository.GetAllAsync();
        }

        public async Task<Course> GetCourseByIdAsync(int courseId)
        {
            return await _courseRepository.GetByIdAsync(courseId);
        }

        public async Task<CourseStatisticsDto> GetCourseStatisticsAsync(int courseId, int? periodId = null)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            var period = await _periodRepository.GetPeriodAsync(periodId);

            var chapters = await _chapterRepository.GetByCourseIdWithAssignmentsAndTestsAsync(courseId, period.Id);

            var assignments = chapters.SelectMany(ch => ch.Assignments).ToList();
            var allCourseTestIds = new HashSet<int>(assignments.SelectMany(a => a.Tests).Select(t => t.Id));
            int totalAssignments = assignments.Count;
            int totalTests = allCourseTestIds.Count;

            var courseStatistics = new CourseStatisticsDto
            {
                CourseId = course.Id,
                CourseCode = course.Code,
                CourseName = course.Name,
                TotalAssignments = totalAssignments,
                TotalTests = totalTests,
                Students = new List<CourseStudentStatisticsDto>()
            };

            if (totalAssignments == 0)
            {
                return courseStatistics;
            }

            var assignmentIds = assignments.Select(a => a.Id).ToArray();
            var lastTestResults = await _testResultRepository.GetLastTestResultsOfAssignmentsAsync(assignmentIds, null);

            var userGroups = lastTestResults
                .Where(tr => tr.User != null)
                .GroupBy(tr => tr.UserId)
                .ToList();

            foreach (var userGroup in userGroups)
            {
                var student = userGroup.First().User;
                var studentResults = userGroup.ToList();

                var studentDto = new CourseStudentStatisticsDto
                {
                    StudentId = student.Id,
                    FirstName = student.FirstName ?? string.Empty,
                    LastName = student.LastName ?? string.Empty,
                    TotalTests = totalTests,
                    Chapters = new List<CourseStudentChapterStatisticsDto>()
                };

                int assignmentsWorkedOn = 0;
                int totalPassedTests = 0;

                foreach (var chapter in chapters.OrderBy(c => (string)c.Code))
                {
                    var chapterDto = new CourseStudentChapterStatisticsDto
                    {
                        ChapterId = chapter.Id,
                        Code = chapter.Code,
                        Description = chapter.Description,
                        Assignments = new List<CourseStudentAssignmentStatisticsDto>()
                    };

                    foreach (var assignment in chapter.Assignments.OrderBy(a => (string)a.Code))
                    {
                        var assignmentTestIds = new HashSet<int>(assignment.Tests.Select(t => t.Id));
                        var assignmentResults = studentResults.Where(tr => tr.Test?.AssignmentId == assignment.Id).ToList();

                        if (assignmentResults.Count > 0)
                        {
                            assignmentsWorkedOn++;
                        }

                        int passedTests = assignmentResults.Count(tr => tr.Passed && assignmentTestIds.Contains(tr.TestId));
                        totalPassedTests += passedTests;

                        var assignmentDto = new CourseStudentAssignmentStatisticsDto
                        {
                            AssignmentId = assignment.Id,
                            Code = assignment.Code,
                            Description = assignment.Description,
                            NumberOfPassedTests = passedTests,
                            NumberOfTests = assignment.Tests.Count
                        };

                        chapterDto.Assignments.Add(assignmentDto);
                    }

                    studentDto.Chapters.Add(chapterDto);
                }

                studentDto.AssignmentsWorkedOn = assignmentsWorkedOn;
                studentDto.TotalPassedTests = totalPassedTests;
                studentDto.CompletionPercentage = totalTests > 0
                    ? Math.Round((double)totalPassedTests / totalTests * 100.0, 2)
                    : 0.0;

                courseStatistics.Students.Add(studentDto);
            }

            courseStatistics.Students = courseStatistics.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            return courseStatistics;
        }
    }
}
