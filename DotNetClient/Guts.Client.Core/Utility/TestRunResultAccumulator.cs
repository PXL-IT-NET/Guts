using Guts.Client.Core.Models;

namespace Guts.Client.Core.Utility;

public class TestRunResultAccumulator
{
    private static readonly Dictionary<string, TestClassRunState> TestClassRunStates = new();
    private static readonly Lock Lock = new();

    public static async Task AddTestResultAsync(TestResult result, ITestClassInfo testClassInfo, ITestOutputWriter outputWriter, 
        Func<ITestClassInfo, IReadOnlyList<TestResult>, Task> onAllTestOfClassCompletedAsync)
    {
        IReadOnlyList<TestResult>? resultsSnapshot;
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
    }
}