namespace Guts.Client.Core.Models;

public class AssignmentTestRun(
    string testClassName,
    Assignment assignment,
    IEnumerable<TestResult> results,
    IEnumerable<SolutionFile> solutionFiles,
    string testCodeHash)
{
    public string TestClassName { get; } = testClassName;

    public Assignment Assignment { get;} = assignment;

    public IEnumerable<TestResult> Results { get; } = results;

    public IEnumerable<SolutionFile> SolutionFiles { get; } = solutionFiles;

    public string TestCodeHash { get; } = testCodeHash;
}