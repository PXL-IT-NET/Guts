using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.CourseAggregate;

[TestFixture]
public class CourseProgressCalculatorTests
{
    private CourseProgressCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new CourseProgressCalculator();
    }

    [Test]
    public void CalculateProgress_NullCourse_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() => _calculator.CalculateProgress(null, [], []));
    }

    [Test]
    public void CalculateProgress_NullOrEmptyChapters_ReturnsReportWithZeroTotals()
    {
        var course = new CourseBuilder().WithId().Build();

        var report = _calculator.CalculateProgress(course, null, null);

        Assert.That(report.CourseId, Is.EqualTo(course.Id));
        Assert.That(report.CourseCode, Is.EqualTo(course.Code));
        Assert.That(report.CourseName, Is.EqualTo(course.Name));
        Assert.That(report.TotalAssignments, Is.EqualTo(0));
        Assert.That(report.TotalTests, Is.EqualTo(0));
        Assert.That(report.Students, Is.Empty);
    }

    [Test]
    public void CalculateProgress_ChaptersWithoutAssignments_ReturnsReportWithZeroTotals()
    {
        var course = new CourseBuilder().WithId().Build();
        var chapter = new Chapter { Id = 1, Code = "ch01", Description = "Chapter 1", Assignments = [] };

        var report = _calculator.CalculateProgress(course, [chapter], []);

        Assert.That(report.TotalAssignments, Is.EqualTo(0));
        Assert.That(report.TotalTests, Is.EqualTo(0));
        Assert.That(report.Students, Is.Empty);
    }

    [Test]
    public void CalculateProgress_ChaptersWithAssignmentsButNoResults_ReturnsReportWithTotalsAndEmptyStudents()
    {
        var course = new CourseBuilder().WithId().Build();
        var assignment = new AssignmentBuilder().WithId(10).WithRandomTests(3).Build();
        var chapter = new Chapter { Id = 1, Code = "ch01", Description = "Chapter 1", Assignments = [assignment] };

        var report = _calculator.CalculateProgress(course, [chapter], []);

        Assert.That(report.TotalAssignments, Is.EqualTo(1));
        Assert.That(report.TotalTests, Is.EqualTo(3));
        Assert.That(report.Students, Is.Empty);
    }

    [Test]
    public void CalculateProgress_MultipleStudents_CalculatesCorrectStatsAndSortsByLastNameThenFirstName()
    {
        var course = new CourseBuilder().WithId(100).WithCode("CS101").WithName("Intro CS").Build();

        var assignment1 = new AssignmentBuilder().WithId(1).WithCode("ex01").WithRandomTests(2).Build();
        var assignment2 = new AssignmentBuilder().WithId(2).WithCode("ex02").WithRandomTests(2).Build();
        var assignment3 = new AssignmentBuilder().WithId(3).WithCode("ex03").WithRandomTests(2).Build();

        var chapter1 = new Chapter { Id = 10, Code = "ch01", Description = "Chapter 1", Assignments = [assignment1, assignment2] };
        var chapter2 = new Chapter { Id = 20, Code = "ch02", Description = "Chapter 2", Assignments = [assignment3] };
        List<Chapter> chapters = [chapter1, chapter2];

        var studentZeta = new User { Id = 1, FirstName = "Bob", LastName = "Zeta" };
        var studentAlphaAlice = new User { Id = 2, FirstName = "Alice", LastName = "Alpha" };
        var studentAlphaBob = new User { Id = 3, FirstName = "Bob", LastName = "Alpha" };

        var a1Tests = assignment1.Tests.ToList();
        var a2Tests = assignment2.Tests.ToList();
        var a3Tests = assignment3.Tests.ToList();

        // Total tests across course = 6 tests (2 + 2 + 2)
        List<TestResult> testResults =
        [
            // Student Alpha Alice: worked on ex01 (2 passed) and ex03 (1 passed, 1 failed) -> 2 worked on, 3 passed of 6 = 50.0%
            new() { UserId = studentAlphaAlice.Id, User = studentAlphaAlice, TestId = a1Tests[0].Id, Test = a1Tests[0], Passed = true },
            new() { UserId = studentAlphaAlice.Id, User = studentAlphaAlice, TestId = a1Tests[1].Id, Test = a1Tests[1], Passed = true },
            new() { UserId = studentAlphaAlice.Id, User = studentAlphaAlice, TestId = a3Tests[0].Id, Test = a3Tests[0], Passed = true },
            new() { UserId = studentAlphaAlice.Id, User = studentAlphaAlice, TestId = a3Tests[1].Id, Test = a3Tests[1], Passed = false },

            // Student Alpha Bob: worked on ex02 (2 passed) -> 1 worked on, 2 passed of 6 = 33.33%
            new() { UserId = studentAlphaBob.Id, User = studentAlphaBob, TestId = a2Tests[0].Id, Test = a2Tests[0], Passed = true },
            new() { UserId = studentAlphaBob.Id, User = studentAlphaBob, TestId = a2Tests[1].Id, Test = a2Tests[1], Passed = true },

            // Student Zeta Bob: worked on ex01 (1 passed) -> 1 worked on, 1 passed of 6 = 16.67%
            new() { UserId = studentZeta.Id, User = studentZeta, TestId = a1Tests[0].Id, Test = a1Tests[0], Passed = true }
        ];

        var report = _calculator.CalculateProgress(course, chapters, testResults);

        Assert.That(report.CourseId, Is.EqualTo(course.Id));
        Assert.That(report.CourseCode, Is.EqualTo("CS101"));
        Assert.That(report.CourseName, Is.EqualTo("Intro CS"));
        Assert.That(report.TotalAssignments, Is.EqualTo(3));
        Assert.That(report.TotalTests, Is.EqualTo(6));
        Assert.That(report.Students, Has.Count.EqualTo(3));

        // Ordering: Alpha Alice, Alpha Bob, Zeta Bob
        var first = report.Students[0];
        Assert.That(first.StudentId, Is.EqualTo(studentAlphaAlice.Id));
        Assert.That(first.LastName, Is.EqualTo("Alpha"));
        Assert.That(first.FirstName, Is.EqualTo("Alice"));
        Assert.That(first.AssignmentsWorkedOn, Is.EqualTo(2));
        Assert.That(first.TotalPassedTests, Is.EqualTo(3));
        Assert.That(first.TotalTests, Is.EqualTo(6));
        Assert.That(first.CompletionPercentage, Is.EqualTo(50.0));
        Assert.That(first.Chapters, Has.Count.EqualTo(2));

        var second = report.Students[1];
        Assert.That(second.StudentId, Is.EqualTo(studentAlphaBob.Id));
        Assert.That(second.LastName, Is.EqualTo("Alpha"));
        Assert.That(second.FirstName, Is.EqualTo("Bob"));
        Assert.That(second.AssignmentsWorkedOn, Is.EqualTo(1));
        Assert.That(second.TotalPassedTests, Is.EqualTo(2));
        Assert.That(second.CompletionPercentage, Is.EqualTo(33.33));

        var third = report.Students[2];
        Assert.That(third.StudentId, Is.EqualTo(studentZeta.Id));
        Assert.That(third.LastName, Is.EqualTo("Zeta"));
        Assert.That(third.FirstName, Is.EqualTo("Bob"));
        Assert.That(third.AssignmentsWorkedOn, Is.EqualTo(1));
        Assert.That(third.TotalPassedTests, Is.EqualTo(1));
        Assert.That(third.CompletionPercentage, Is.EqualTo(16.67));

        // Verify chapter breakdown for first student
        var ch1Progress = first.Chapters.First(c => c.ChapterId == chapter1.Id);
        Assert.That(ch1Progress.Assignments, Has.Count.EqualTo(2));
        var a1Progress = ch1Progress.Assignments.First(a => a.AssignmentId == assignment1.Id);
        Assert.That(a1Progress.NumberOfPassedTests, Is.EqualTo(2));
        Assert.That(a1Progress.NumberOfTests, Is.EqualTo(2));
        var a2Progress = ch1Progress.Assignments.First(a => a.AssignmentId == assignment2.Id);
        Assert.That(a2Progress.NumberOfPassedTests, Is.EqualTo(0));
        Assert.That(a2Progress.NumberOfTests, Is.EqualTo(2));
    }
}

