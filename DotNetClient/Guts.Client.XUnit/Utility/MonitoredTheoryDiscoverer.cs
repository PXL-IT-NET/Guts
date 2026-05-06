using Xunit;
using Xunit.Sdk;
using Xunit.v3;
namespace Guts.Client.XUnit.Utility;

public class MonitoredTheoryDiscoverer : TheoryDiscoverer
{
    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object?[] testMethodArguments)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(
            discoveryOptions,
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments);

        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);

        return new([
            new MonitoredXunitTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                traits,
                testMethodArguments,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout)
        ]);
    }
}
