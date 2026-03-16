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

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo theoryAttribute, object[] dataRow)
    {
        string? displayName = theoryAttribute.GetNamedArgument<string>(nameof(TheoryAttribute.DisplayName));

        yield return new MonitoredXunitTestCase(
            _diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            displayName,
            dataRow);
    }
}
