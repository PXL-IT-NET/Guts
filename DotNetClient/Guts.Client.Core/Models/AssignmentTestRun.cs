namespace Guts.Client.Core.Models;

public class AssignmentTestRun(
    Assignment assignment,
    IEnumerable<TestResult> results,
    IEnumerable<SolutionFile> solutionFiles,
    string testCodeHash)
{
    public Assignment Assignment { get;} = assignment;

    public IEnumerable<TestResult> Results { get; } = results;

    public IEnumerable<SolutionFile> SolutionFiles { get; } = solutionFiles;

    public string TestCodeHash { get; } = testCodeHash;
}