using Guts.Client.Core.Models;

namespace Guts.Client.XUnit;

[AttributeUsage(AttributeTargets.Class)]
public class ExerciseTestClassAttribute : MonitoredTestClassBaseAttribute
{
    private readonly string _chapterCode;
    private readonly string _exerciseCode;

    public ExerciseTestClassAttribute(string courseCode, string chapterCode, string exerciseCode) : base(courseCode)
    {
        _chapterCode = chapterCode;
        _exerciseCode = exerciseCode;
        SourceCodeRelativeFilePaths = null;
    }

    public ExerciseTestClassAttribute(string courseCode, string chapterCode, string exerciseCode, string sourceCodeRelativeFilePaths)
        : this(courseCode, chapterCode, exerciseCode)
    {
        SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
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

    protected override TestRunType RunType => TestRunType.ForExercise;
}
