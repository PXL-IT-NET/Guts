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


public class TestRunResultAccumulator2
{
    private static readonly Dictionary<string, TestClassRunState> TestClassRunStates = new();
    private static readonly Lock Lock = new();

    //public ITestClassInfo TestClass { get; }
    //public IList<TestResult> Results { get; }
    //public string TestClassHash { get; set; }


    public static async Task AddTestResultAsync(TestResult result, ITestClassInfo testClassInfo, ITestOutputWriter outputWriter, 
        Func<ITestClassInfo, IReadOnlyList<TestResult>, Task> onAllTestOfClassCompletedAsync)
    {
        IReadOnlyList<TestResult>? resultsSnapshot = null;
        lock (Lock)
        {
            if (!TestClassRunStates.TryGetValue(testClassInfo.Name, out var runState))
            {
                runState = AddRunStateEntryForTestClass(testClassInfo, outputWriter);
            }

            if (runState.Completed) return;
            if (runState.Results.Any(r => r.TestName == result.TestName)) return;

            runState.Results.Add(result);

            if (runState.Results.Count < runState.TestClass.NumberOfTests) return;

            runState.Completed = true;
            resultsSnapshot = runState.Results.ToList();
        }

        await onAllTestOfClassCompletedAsync(testClassInfo, resultsSnapshot);
    }

    private static TestClassRunState AddRunStateEntryForTestClass(ITestClassInfo testClassInfo, ITestOutputWriter outputWriter)
    {
        var runState = new TestClassRunState(testClassInfo, outputWriter);
        TestClassRunStates[testClassInfo.Name] = runState;
        return runState;
    }
}

internal class TestClassRunState
{
    public ITestClassInfo TestClass { get; }
    public List<TestResult> Results { get; }
    public bool Completed { get; set; }

    internal TestClassRunState(ITestClassInfo classInfo, ITestOutputWriter outputWriter)
    {
        TestClass = classInfo;
        Results = new List<TestResult>();
        Completed = false;
       // TestClassHash = GetTestClassHash(outputWriter);
    }

    //private string GetTestClassHash(ITestOutputWriter outputWriter)
    //{
    //    var testCodeHash = string.Empty;

    //    FileInfo? fileInfo = TestClass.TestProjectDirectory
    //        .GetFiles(TestClass.Name + ".cs", SearchOption.AllDirectories).FirstOrDefault();

    //    if (fileInfo is null)
    //    {
    //        outputWriter.WriteError(
    //            $"Could not find test code file for test class {TestClass.Name}. " +
    //            $"Searched for {TestClass.Name}.cs in directory {TestClass.TestProjectDirectory.FullName} (and subdirectories)");
    //    }
    //    else
    //    {
    //        testCodeHash = FileUtil.GetFileHash(fileInfo.FullName);
    //    }
    //    return testCodeHash;
    //}
}