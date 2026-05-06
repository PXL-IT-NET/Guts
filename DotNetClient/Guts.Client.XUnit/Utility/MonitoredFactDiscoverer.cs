using Xunit.Sdk;
using Xunit.v3;

namespace Guts.Client.XUnit.Utility;

public class MonitoredFactDiscoverer : FactDiscoverer
{
    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(
            discoveryOptions,
            testMethod,
            factAttribute,
            [],
            null,
            factAttribute.DisplayName);

        return new MonitoredXunitTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.Explicit,
            details.SkipExceptions,
            details.SkipReason,
            details.SkipType,
            details.SkipUnless,
            details.SkipWhen,
            testMethod.Traits.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase),
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            timeout: details.Timeout);
    }
}
