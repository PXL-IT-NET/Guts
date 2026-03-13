using Guts.Client.Core.Models;

namespace Guts.Client.XUnit;

[AttributeUsage(AttributeTargets.Class)]
public class ProjectComponentTestClassAttribute : MonitoredTestClassBaseAttribute
{
    private readonly string _projectCode;
    private readonly string _componentCode;

    public ProjectComponentTestClassAttribute(string courseCode, string projectCode, string componentCode) : base(courseCode)
    {
        _projectCode = projectCode;
        _componentCode = componentCode;
        SourceCodeRelativeFilePaths = null;
    }

    public ProjectComponentTestClassAttribute(string courseCode, string projectCode, string componentCode, string sourceCodeRelativeFilePaths)
        : this(courseCode, projectCode, componentCode)
    {
        SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
    }

    protected override Assignment CreateAssignment()
    {
        return new Assignment
        {
            CourseCode = CourseCode,
            TopicCode = _projectCode,
            AssignmentCode = _componentCode
        };
    }

    protected override TestRunType RunType => TestRunType.ForProject;
}
