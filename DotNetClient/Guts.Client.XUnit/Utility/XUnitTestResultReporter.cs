using Guts.Client.Core.Models;
using Guts.Client.Core.TestTools;
using Guts.Client.Core.Utility;
using Xunit.Abstractions;

namespace Guts.Client.XUnit.Utility;

internal static class XUnitTestResultReporter
{
    private static readonly Dictionary<string, XUnitTestRunState> TestRunStateByClassName = new();
    private static readonly object Lock = new();

    public static void Report(ITestMethod testMethod, string testName, bool passed, string message)
    {
        var testClassType = testMethod.TestClass.Class.ToRuntimeType();
        var className = testClassType.FullName ?? testClassType.Name;

        lock (Lock)
        {
            if (!TestRunStateByClassName.TryGetValue(className, out var runState))
            {
                var classInfo = XUnitTestClassInfo.CreateFromTestMethod(testMethod);
                runState = XUnitTestRunState.Create(classInfo);
                TestRunStateByClassName[className] = runState;
            }

            if (runState.Sent) return;
            if (runState.TestResults.Any(r => r.TestName == testName)) return;

            runState.TestResults.Add(new TestResult(testName, passed, message?.Trim() ?? string.Empty));

            XUnitTestOutputWriter.Instance.WriteProgress(
                $"You ran {runState.TestResults.Count} tests of {runState.NumberOfTestsInCurrentClass} tests in the test class '{runState.TestClassName}'");

            if (runState.TestResults.Count < runState.NumberOfTestsInCurrentClass) return;

            var monitoredClassAttribute = testClassType
                .GetCustomAttributes(inherit: true)
                .OfType<MonitoredTestClassBaseAttribute>()
                .FirstOrDefault();

            if (monitoredClassAttribute is null)
            {
                runState.Sent = true;
                return;
            }

            monitoredClassAttribute.SendTestResults(runState);
            runState.Sent = true;
        }
    }
}

internal class XUnitTestRunState
{
    public string TestClassName { get; }
    public int NumberOfTestsInCurrentClass { get; }
    public string TestCodeHash { get; }
    public IList<TestResult> TestResults { get; }
    public bool Sent { get; set; }

    private XUnitTestRunState(string testClassName, int numberOfTestsInCurrentClass, string testCodeHash)
    {
        TestClassName = testClassName;
        NumberOfTestsInCurrentClass = numberOfTestsInCurrentClass;
        TestCodeHash = testCodeHash;
        TestResults = new List<TestResult>();
    }

    public static XUnitTestRunState Create(ITestClassInfo classInfo)
    {
        var testCodeHash = string.Empty;

        FileInfo? fileInfo = classInfo.TestProjectDirectory
            .GetFiles(classInfo.Name + ".cs", SearchOption.AllDirectories).FirstOrDefault();

        if (fileInfo is null)
        {
            XUnitTestOutputWriter.Instance.WriteError(
                $"Could not find test code file for test class {classInfo.Name}. " +
                $"Searched for {classInfo.Name}.cs in directory {classInfo.TestProjectDirectory.FullName} (and subdirectories)");
        }
        else
        {
            testCodeHash = FileUtil.GetFileHash(fileInfo.FullName);
        }

        return new XUnitTestRunState(classInfo.Name, classInfo.NumberOfTests, testCodeHash);
    }
}
