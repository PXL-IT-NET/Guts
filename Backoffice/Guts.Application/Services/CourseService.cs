using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Services;

internal class CourseService(
    ICourseRepository courseRepository,
    ICourseFactory courseFactory,
    IPeriodRepository periodRepository,
    IChapterRepository chapterRepository,
    ITestResultRepository testResultRepository,
    ICourseProgressCalculator courseProgressCalculator)
    : ICourseService
{
    public async Task<IReadOnlyList<Course>> GetAllCoursesAsync()
    {
        return await courseRepository.GetAllAsync();
    }

    public async Task<Course> GetCourseByIdAsync(int courseId)
    {
        return await courseRepository.GetByIdAsync(courseId);
    }

    public async Task<Course> CreateCourseAsync(string code, string name)
    {
        IReadOnlyList<Course> existingCourses = await courseRepository.GetAllAsync();
        Course course = courseFactory.CreateNew(code, name, existingCourses);
        return await courseRepository.AddAsync(course);
    }

    public async Task<Course> UpdateCourseAsync(int courseId, string code, string name)
    {
        IReadOnlyList<Course> allCourses = await courseRepository.GetAllAsync();
        Course course = await courseRepository.GetByIdAsync(courseId);
        course.Update(code, name, allCourses);
        await courseRepository.UpdateAsync(course);
        return course;
    }

    public async Task DeleteCourseAsync(int courseId)
    {
        Course course = await courseRepository.GetByIdAsync(courseId);
        await courseRepository.DeleteAsync(course);
    }

    public async Task<CourseStatisticsDto> GetCourseStatisticsAsync(int courseId, int? periodId = null)
    {
        var course = await courseRepository.GetByIdAsync(courseId);
        var period = await periodRepository.GetPeriodAsync(periodId);

        var chapters = await chapterRepository.GetByCourseIdWithAssignmentsAndTestsAsync(courseId, period.Id);

        var assignments = chapters.SelectMany(ch => ch.Assignments ?? []).ToList();
        IList<TestResult> lastTestResults = [];
        if (assignments.Count > 0)
        {
            var assignmentIds = assignments.Select(a => a.Id).ToArray();
            lastTestResults = await testResultRepository.GetLastTestResultsOfAssignmentsAsync(assignmentIds, null);
        }

        var report = courseProgressCalculator.CalculateProgress(course, [.. chapters], [.. lastTestResults]);

        return new CourseStatisticsDto
        {
            CourseId = report.CourseId,
            CourseCode = report.CourseCode,
            CourseName = report.CourseName,
            TotalAssignments = report.TotalAssignments,
            TotalTests = report.TotalTests,
            Students = report.Students.Select(s => new CourseStudentStatisticsDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                TotalTests = s.TotalTests,
                TotalPassedTests = s.TotalPassedTests,
                AssignmentsWorkedOn = s.AssignmentsWorkedOn,
                CompletionPercentage = s.CompletionPercentage,
                Chapters = s.Chapters.Select(c => new CourseStudentChapterStatisticsDto
                {
                    ChapterId = c.ChapterId,
                    Code = c.Code,
                    Description = c.Description,
                    Assignments = c.Assignments.Select(a => new CourseStudentAssignmentStatisticsDto
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
    }
}