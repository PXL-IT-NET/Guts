using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Guts.Client.XUnit.Utility;

public class MonitoredFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        var displayName = factAttribute.GetNamedArgument<string>(nameof(FactAttribute.DisplayName));

        yield return new MonitoredXunitTestCase(
            diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            displayName);
    }
}
