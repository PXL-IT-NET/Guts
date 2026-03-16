using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using System.Data;
using Xunit.Abstractions;
using Xunit.Sdk;
using static System.Net.Mime.MediaTypeNames;

namespace Guts.Client.XUnit.Utility;

public class MonitoredXunitTestCase : XunitTestCase
{
    private string? _displayNameOverride;

    [Obsolete("Called by the de-serializer", true)]
    public MonitoredXunitTestCase()
    {
    }

    public MonitoredXunitTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        string? displayNameOverride,
        object[] arguments)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, arguments)
    {
        _displayNameOverride = displayNameOverride;
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        base.Serialize(data);
        data.AddValue(nameof(_displayNameOverride), _displayNameOverride);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        base.Deserialize(data);
        _displayNameOverride = data.GetValue<string>(nameof(_displayNameOverride));
    }

    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var capturingMessageBus = new CapturingMessageBus(messageBus);
        RunSummary? summary = await base.RunAsync(
            diagnosticMessageSink,
            capturingMessageBus,
            constructorArguments,
            aggregator,
            cancellationTokenSource);

        bool passed = summary.Failed == 0;
        string message = passed ? string.Empty : capturingMessageBus.FailureMessage;

        string testName = string.IsNullOrEmpty(_displayNameOverride)
            ? new CamelCaseConverter().ToNormalSentence(TestMethod.Method.Name)
            : _displayNameOverride;

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


        //XUnitTestResultReporter.Report(TestMethod, displayName, passed, message);

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
