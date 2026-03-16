using System.Data;
using Xunit.Abstractions;
using Xunit.Sdk;

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
        string? displayName = string.IsNullOrWhiteSpace(_displayNameOverride) ? DisplayName : _displayNameOverride;

        XUnitTestResultReporter.Report(TestMethod, displayName, passed, message);

        return summary;
    }
}
