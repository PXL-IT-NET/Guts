using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using Xunit.Sdk;
using Xunit.v3;

namespace Guts.Client.XUnit.Utility;

public class MonitoredXunitTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public MonitoredXunitTestCase()
    {
    }

    public MonitoredXunitTestCase(
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueId,
        bool @explicit,
        Type[]? skipExceptions,
        string? skipReason,
        Type? skipType,
        string? skipUnless,
        string? skipWhen,
        Dictionary<string, HashSet<string>>? traits,
        object?[]? testMethodArguments = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        int? timeout = null)
        : base(testMethod, testCaseDisplayName, uniqueId, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
    {
    }

    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var capturingMessageBus = new CapturingMessageBus(messageBus);
        RunSummary summary = await XunitRunnerHelper.RunXunitTestCase(
            this,
            capturingMessageBus,
            cancellationTokenSource,
            aggregator,
            explicitOption,
            constructorArguments);

        bool passed = summary.Failed == 0;
        string message = passed ? string.Empty : capturingMessageBus.FailureMessage;

        string testName = new CamelCaseConverter().ToNormalSentence(TestMethod.Method.Name);

        string className = TestMethod.TestClass.Class.Name;

        int dotIndex = className.LastIndexOf('.');
        if (dotIndex >= 0)
        {
            className = className.Substring(dotIndex + 1);
        }
        testName = $"({className}) {testName}";

        if (TestMethodArguments.Any())
        {

            testName = $"{testName} ({string.Join(", ", TestMethodArguments.Select(FormatArgument))})";
        }

        var result = new TestResult(testName, passed, message);

        XUnitTestClassInfo classInfo = XUnitTestClassInfo.CreateFromTestMethod(TestMethod);

        await TestRunResultAccumulator.AddTestResultAsync(result, classInfo, XUnitTestOutputWriter.Instance, OnAllTestOfClassCompletedAsync);

        return summary;
    }

    private async Task OnAllTestOfClassCompletedAsync(ITestClassInfo testClassInfo, IReadOnlyList<TestResult> results)
    {
        // Find the testclass attribute
        MonitoredTestClassBaseAttribute? monitoredClassAttribute = testClassInfo.Type
            .GetCustomAttributes(inherit: true)
            .OfType<MonitoredTestClassBaseAttribute>()
            .FirstOrDefault();

        if (monitoredClassAttribute is null)
        {
            XUnitTestOutputWriter.Instance.WriteProgress(
                $"Cannot find a test class attribute ('ExerciseTestClass' or 'ProjectComponentTestClass' on class {testClassInfo.Name}. " +
                "The test results will therefore not be sent to the GUTS system.");
        }
        else
        {
            await monitoredClassAttribute!.SendTestResults(testClassInfo, results);
        }
    }

    private static string FormatArgument(object? argument)
    {
        return argument?.ToString() ?? "null";
    }
}
