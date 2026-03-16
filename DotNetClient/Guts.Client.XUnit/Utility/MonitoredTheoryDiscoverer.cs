using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Guts.Client.XUnit.Utility;

public class MonitoredTheoryDiscoverer : TheoryDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;

    public MonitoredTheoryDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    protected override IXunitTestCase CreateTestCaseForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo theoryAttribute,
        object[] dataRow)
    {
        string? displayName = theoryAttribute.GetNamedArgument<string>(nameof(TheoryAttribute.DisplayName));
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            displayName = $"{displayName} ({string.Join(", ", dataRow.Select(FormatArgument))})";
        }

        return new MonitoredXunitTestCase(
            _diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            displayName,
            dataRow);
    }

    private static string FormatArgument(object? argument)
    {
        return argument?.ToString() ?? "null";
    }
}
