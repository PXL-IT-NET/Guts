using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.Repositories;

namespace Guts.Infrastructure.Tests.Repositories;

internal sealed class TestDataSeeder
{
    private readonly Func<GutsContext> _contextFactory;

    public TestDataSeeder(Func<GutsContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Course> SeedCourseAsync(string code = null, string name = null)
    {
        await using var context = _contextFactory();
        var course = new Course
        {
            Code = code ?? $"C_{Random.Shared.NextString()}",
            Name = name ?? $"Course_{Random.Shared.NextString()}"
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();
        return course;
    }

    public async Task<Period> SeedPeriodAsync(DateTime? from = null, DateTime? until = null, string description = null)
    {
        await using var context = _contextFactory();
        var period = new PeriodBuilder()
            .WithRange(from ?? DateTime.Today.AddDays(-5), until ?? DateTime.Today.AddDays(30))
            .Build();
        if (!string.IsNullOrEmpty(description))
        {
            period.Description = description;
        }

        context.Periods.Add(period);
        await context.SaveChangesAsync();
        return period;
    }

    public async Task<(Course Course, Period Period)> SeedCourseAndActivePeriodAsync(string courseCode = "NET1")
    {
        var course = await SeedCourseAsync(courseCode, ".NET Course");
        var period = await SeedPeriodAsync();
        return (course, period);
    }

    public async Task<User> SeedUserAsync(string firstName = "Alice", string lastName = "Smith", string email = null)
    {
        await using var context = _contextFactory();
        var user = new User
        {
            UserName = email ?? $"{firstName.ToLowerInvariant()}_{Guid.NewGuid():N}",
            Email = email ?? $"{firstName.ToLowerInvariant()}@student.pxl.be",
            FirstName = firstName,
            LastName = lastName
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<Chapter> SeedChapterAsync(
        int courseId,
        int periodId,
        string code = "Ch01",
        string description = "Chapter 1",
        string assignmentCode = "Ex01",
        params string[] testNames)
    {
        await using var context = _contextFactory();
        var chapter = new Chapter
        {
            Code = code,
            Description = description,
            CourseId = courseId,
            PeriodId = periodId,
            Assignments = new List<Assignment>()
        };

        if (!string.IsNullOrEmpty(assignmentCode))
        {
            var names = testNames.Length > 0 ? testNames : new[] { "Test_1" };
            chapter.Assignments.Add(new Assignment
            {
                Code = assignmentCode,
                Description = $"Assignment {assignmentCode}",
                Tests = names.Select(n => new Test { TestName = n }).ToList(),
                TestCodeHashes = new List<TestCodeHash> { new() { Hash = $"HASH_{assignmentCode}" } }
            });
        }

        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();
        return chapter;
    }

    public async Task<Project> SeedProjectAsync(
        int courseId,
        int periodId,
        string code = "Proj1",
        string description = "Project 1",
        string assignmentCode = "Comp1",
        string[] testNames = null,
        params (string TeamName, int[] UserIds)[] teams)
    {
        await using var context = _contextFactory();
        var names = testNames is { Length: > 0 } ? testNames : new[] { "Test_1" };

        var project = new Project
        {
            Code = code,
            Description = description,
            CourseId = courseId,
            PeriodId = periodId,
            Assignments = new List<Assignment>
            {
                new()
                {
                    Code = assignmentCode,
                    Description = $"Component {assignmentCode}",
                    Tests = names.Select(n => new Test { TestName = n }).ToList()
                }
            },
            Teams = teams.Select(t => (IProjectTeam)new ProjectTeam
            {
                Name = t.TeamName,
                TeamUsers = t.UserIds.Select(uid => (IProjectTeamUser)new ProjectTeamUser { UserId = uid }).ToList()
            }).ToList()
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    public async Task<ProjectAssessment> SeedProjectAssessmentAsync(int projectId, string description = "Sprint 1 Assessment")
    {
        await using var context = _contextFactory();
        var factory = new ProjectAssessment.Factory();
        var assessment = (ProjectAssessment)factory.CreateNew(
            projectId,
            description,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(5));
        context.Set<ProjectAssessment>().Add(assessment);
        await context.SaveChangesAsync();
        return assessment;
    }

    public async Task<ProjectTeamAssessment> SeedProjectTeamAssessmentAsync(
        int projectAssessmentId,
        int teamId,
        (int UserId, int SubjectId)? initialPeerEvaluation = null)
    {
        await using var context = _contextFactory();
        var loadedAssessment = await context.Set<ProjectAssessment>().FindAsync(projectAssessmentId);
        var teamRepo = new ProjectTeamDbRepository(context);
        var loadedTeam = await teamRepo.LoadByIdAsync(teamId);

        var ptaFactory = new ProjectTeamAssessment.Factory();
        var teamAssessment = (ProjectTeamAssessment)ptaFactory.CreateNew(loadedAssessment, loadedTeam);
        context.ProjectTeamAssessments.Add(teamAssessment);
        await context.SaveChangesAsync();

        if (initialPeerEvaluation.HasValue)
        {
            teamAssessment.AddOrUpdatePeerAssessment(
                initialPeerEvaluation.Value.UserId,
                initialPeerEvaluation.Value.SubjectId,
                AssessmentScore.Average,
                AssessmentScore.Average,
                AssessmentScore.Average,
                "Good");
            await context.SaveChangesAsync();
        }

        return teamAssessment;
    }

    public async Task<(Exam Exam, ExamPart ExamPart)> SeedExamWithPartAndEvaluationAsync(
        int courseId,
        int periodId,
        Assignment assignment,
        string examName = "Midterm Exam",
        string partName = "Part 1")
    {
        await using var context = _contextFactory();
        var exam = new Exam.Factory().CreateNew(courseId, periodId, examName);
        context.Exams.Add(exam);
        await context.SaveChangesAsync();

        var examPart = (ExamPart)exam.AddExamPart(partName, DateTime.UtcNow.AddDays(1));
        await context.SaveChangesAsync();

        examPart.AddAssignmentEvaluation(assignment, 10, 0);
        await context.SaveChangesAsync();

        return (exam, examPart);
    }

    public async Task<TestRun> SeedTestRunAsync(
        int assignmentId,
        int userId,
        DateTime createDateTimeUtc,
        params (int TestId, bool Passed)[] results)
    {
        await using var context = _contextFactory();
        var run = new TestRun
        {
            AssignmentId = assignmentId,
            UserId = userId,
            CreateDateTime = createDateTimeUtc,
            TestResults = results.Select(r => new TestResult
            {
                TestId = r.TestId,
                UserId = userId,
                Passed = r.Passed,
                Message = r.Passed ? string.Empty : "Failed",
                CreateDateTime = createDateTimeUtc
            }).ToList()
        };
        context.TestRuns.Add(run);
        await context.SaveChangesAsync();
        return run;
    }

    public static SolutionFile CreateSolutionFile(int assignmentId, int userId, string filePath, DateTime modifyDateTimeUtc, string content)
    {
        var file = new SolutionFileBuilder()
            .WithAssignmentId(assignmentId)
            .WithUserId(userId)
            .WithPath(new FilePath(filePath))
            .WithContent(content)
            .Build();

        typeof(SolutionFile).GetProperty(nameof(SolutionFile.ModifyDateTime))!.SetValue(file, modifyDateTimeUtc);
        return file;
    }
}
