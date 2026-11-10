using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;

namespace Guts.Domain.CourseAggregate;

public class CourseProgressCalculator : ICourseProgressCalculator
{
    public CourseProgressReport CalculateProgress(
        Course course,
        IReadOnlyCollection<Chapter> chapters,
        IReadOnlyCollection<TestResult> testResults)
    {
        Contracts.Require(course != null, "Course cannot be null.");
        chapters ??= [];
        testResults ??= [];

        var assignments = chapters.SelectMany(ch => ch.Assignments ?? []).ToList();
        var allCourseTestIds = new HashSet<int>(assignments
            .SelectMany(a => a.Tests ?? Enumerable.Empty<Test>())
            .Select(t => t.Id));

        int totalAssignments = assignments.Count;
        int totalTests = allCourseTestIds.Count;

        if (totalAssignments == 0)
        {
            return new CourseProgressReport(
                course.Id,
                course.Code,
                course.Name,
                0,
                0,
                []);
        }

        var userGroups = testResults
            .Where(tr => tr.User != null)
            .GroupBy(tr => tr.UserId)
            .ToList();

        var studentProgressList = new List<CourseStudentProgress>();

        foreach (var userGroup in userGroups)
        {
            var student = userGroup.First().User;
            var studentResults = userGroup.ToList();

            int assignmentsWorkedOn = 0;
            int totalPassedTests = 0;
            var chapterProgressList = new List<CourseChapterProgress>();

            foreach (var chapter in chapters.OrderBy(c => (string)c.Code))
            {
                var assignmentProgressList = new List<CourseAssignmentProgress>();

                foreach (var assignment in (chapter.Assignments ?? Enumerable.Empty<Assignment>()).OrderBy(a => a.Code))
                {
                    var tests = assignment.Tests ?? [];
                    var assignmentTestIds = new HashSet<int>(tests.Select(t => t.Id));
                    var assignmentResults = studentResults.Where(tr => tr.Test?.AssignmentId == assignment.Id).ToList();

                    if (assignmentResults.Count > 0)
                    {
                        assignmentsWorkedOn++;
                    }

                    int passedTests = assignmentResults.Count(tr => tr.Passed && assignmentTestIds.Contains(tr.TestId));
                    totalPassedTests += passedTests;

                    assignmentProgressList.Add(new CourseAssignmentProgress(
                        assignment.Id,
                        assignment.Code,
                        assignment.Description,
                        passedTests,
                        tests.Count));
                }

                chapterProgressList.Add(new CourseChapterProgress(
                    chapter.Id,
                    chapter.Code,
                    chapter.Description,
                    assignmentProgressList));
            }

            double completionPercentage = totalTests > 0
                ? Math.Round((double)totalPassedTests / totalTests * 100.0, 2)
                : 0.0;

            studentProgressList.Add(new CourseStudentProgress
            {
                StudentId = student.Id,
                FirstName = student.FirstName ?? string.Empty,
                LastName = student.LastName ?? string.Empty,
                TotalTests = totalTests,
                TotalPassedTests = totalPassedTests,
                AssignmentsWorkedOn = assignmentsWorkedOn,
                CompletionPercentage = completionPercentage,
                Chapters = chapterProgressList
            });
        }

        var sortedStudents = studentProgressList
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToList();

        return new CourseProgressReport(
            course.Id,
            course.Code,
            course.Name,
            totalAssignments,
            totalTests,
            sortedStudents);
    }
}
