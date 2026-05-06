using Guts.Client.Core.Models;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.NUnit;

[AttributeUsage(AttributeTargets.Class)]
public class ExerciseTestFixtureAttribute : MonitoredTestFixtureBaseAttribute
{
    private readonly string _chapterCode;
    private readonly string _exerciseCode;

    public ExerciseTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode) : base(courseCode)
    {
        _chapterCode = chapterCode;
        _exerciseCode = exerciseCode;
        SourceCodeRelativeFilePaths = null;
    }

    public ExerciseTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode, string sourceCodeRelativeFilePaths) : this(courseCode, chapterCode, exerciseCode)
    {
        SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
    }

    protected override TestRunType RunType => TestRunType.ForExercise;

    public override void BeforeTest(ITest test)
    {
        base.BeforeTest(test);
        TestContext.Progress.WriteLine($"Starting test run. Chapter {_chapterCode}, exercise {_exerciseCode}");
    }

    protected override Assignment CreateAssignment()
    {
        return new Assignment
        {
            CourseCode = CourseCode,
            TopicCode = _chapterCode,
            AssignmentCode = _exerciseCode
        };
    }
}