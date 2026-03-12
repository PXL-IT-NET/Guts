using Guts.Client.Core.Models;
using Guts.Client.Core.TestTools;

namespace Guts.Client.Core.Utility;

public class TestRunResultAccumulator
{
    public IList<TestResult> TestResults { get; }

    public int NumberOfTestsInCurrentFixture { get; set; }

    public string TestClassName { get; set; }

    public string TestCodeHash { get; set; }

    private TestRunResultAccumulator()
    {
        TestClassName = string.Empty;
        TestCodeHash = string.Empty;
        TestResults = new List<TestResult>();
        Clear();
    }

    public static TestRunResultAccumulator Instance => field ??= new TestRunResultAccumulator();

    public void AddTestResult(TestResult result, ITestClassInfo? testClassInfo, ITestOutputWriter outputWriter)
    {
        EnsureMetaDataIsLoaded(testClassInfo, outputWriter);

        if (TestResults.Any(r => r.TestName == result.TestName)) return; //avoid duplicated (repeated) tests

        TestResults.Add(result);
    }

    public void Clear()
    {
        TestResults.Clear();
        NumberOfTestsInCurrentFixture = 0;
        TestClassName = string.Empty;
        TestCodeHash = string.Empty;
    }

    private void EnsureMetaDataIsLoaded(ITestClassInfo? testClassInfo, ITestOutputWriter outputWriter)
    {
        if (NumberOfTestsInCurrentFixture > 0) return;
        if (testClassInfo is null) return;

        TestClassName = testClassInfo.Name;

        //Find test code file in test project directory
        FileInfo? fileInfo = testClassInfo.TestProjectDirectory
            .GetFiles(TestClassName + ".cs", SearchOption.AllDirectories).FirstOrDefault();

        if (fileInfo is null)
        {
            outputWriter.WriteError(
                $"Could not find test code file for test class {TestClassName}. " +
                $"Searched for {TestClassName}.cs in directory {testClassInfo.TestProjectDirectory.FullName} (and subdirectories)");
        }
        else
        {
            TestCodeHash = FileUtil.GetFileHash(fileInfo.FullName);
        }

        NumberOfTestsInCurrentFixture = testClassInfo.NumberOfTests;
    }
}