using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using Guts.Client.NUnit.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.NUnit;

public class MonitoredTestAttribute : TestAttribute, ITestAction
{
    private readonly string? _displayName;

    public ActionTargets Targets => ActionTargets.Test;

    public MonitoredTestAttribute()
    {
        _displayName = null;
    }

    public MonitoredTestAttribute(string displayName)
    {
        _displayName = displayName;
    }

    public void BeforeTest(ITest test)
    {
        //do nothing before
    }

    public void AfterTest(ITest test)
    {
        string testName = _displayName ?? string.Empty;
        if (string.IsNullOrEmpty(testName))
        {
            testName = new CamelCaseConverter().ToNormalSentence(test.MethodName);
            if (!string.IsNullOrEmpty(test.ClassName))
            {
                string className = test.ClassName;
                int dotIndex = className.LastIndexOf('.');
                if (dotIndex >= 0)
                {
                    className = className.Substring(dotIndex + 1);
                }
                testName = $"({className}) {testName}";
            }
        }
            
        if (IsTestCase(test))
        {
            testName += $" (Case {GetTestCaseNumber(test)})";
        }

        var resultAdapter = TestContext.CurrentContext.Result;
        var result = new TestResult(
            testName,
            passed:Equals(resultAdapter.Outcome, ResultState.Success),
            message:(resultAdapter.Message ?? string.Empty).Trim()
        );

        ITestClassInfo testClassInfo = NUnitTestFixtureInfo.CreateFromTest(test);
        TestRunResultAccumulator.AddTestResultAsync(result, testClassInfo, NUnitTestOutputWriter.Instance, OnAllTestOfClassCompletedAsync).Wait();
    }

    private async Task OnAllTestOfClassCompletedAsync(ITestClassInfo testClassInfo, IReadOnlyList<TestResult> results)
    {
        // Find the testclass attribute
        MonitoredTestFixtureBaseAttribute? monitoredFixtureAttribute = testClassInfo.Type
            .GetCustomAttributes(inherit: true)
            .OfType<MonitoredTestFixtureBaseAttribute>()
            .FirstOrDefault();

        if (monitoredFixtureAttribute is null)
        {
            NUnitTestOutputWriter.Instance.WriteError(
                $"Cannot find a test class attribute ('ExerciseTestClass' or 'ProjectComponentTestClass' on class {testClassInfo.Name}");
        }

        await monitoredFixtureAttribute!.SendTestResults(testClassInfo, results);
    }

    private int GetTestCaseNumber(ITest test)
    {
        int testCaseNumber = 1;
        bool found = false;
        ITest parentTest = test.Parent!;
        while (!found && testCaseNumber <= parentTest.TestCaseCount)
        {
            if (parentTest.Tests[testCaseNumber - 1].Id == test.Id)
            {
                found = true;
            }
            else
            {
                testCaseNumber++;
            }
        }

        return testCaseNumber;
    }

    private bool IsTestCase(ITest test)
    {
        return test.Arguments != null && test.Arguments.Length > 0;
    }
}