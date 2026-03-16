using Guts.Client.Core.Models;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.NUnit;

[AttributeUsage(AttributeTargets.Class)]
public class ProjectComponentTestFixtureAttribute : MonitoredTestFixtureBaseAttribute
{
    private readonly string _projectCode;
    private readonly string _componentCode;
    private readonly string _courseCode;

    public ProjectComponentTestFixtureAttribute(string courseCode, string projectCode, string componentCode) : base(courseCode)
    {
        _courseCode = courseCode;
        _projectCode = projectCode;
        _componentCode = componentCode;
        SourceCodeRelativeFilePaths = null;
    }

    public ProjectComponentTestFixtureAttribute(string courseCode, string projectCode, string componentCode, string sourceCodeRelativeFilePaths) : this(courseCode, projectCode, componentCode)
    {
        SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
    }

    protected override TestRunType RunType => TestRunType.ForProject;

    public override void BeforeTest(ITest test)
    {
        base.BeforeTest(test);
        TestContext.Progress.WriteLine($"Starting test run. Project '{_projectCode}', component '{_componentCode}'.");
    }

    protected override Assignment CreateAssignment()
    {
        return new Assignment
        {
            AssignmentCode = _projectCode,
            CourseCode = _courseCode,
            TopicCode = _componentCode
        };
    }
}