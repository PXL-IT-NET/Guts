using Xunit;
using Xunit.Sdk;

namespace Guts.Client.XUnit;

[XunitTestCaseDiscoverer("Guts.Client.XUnit.Utility.MonitoredTheoryDiscoverer", "Guts.Client.XUnit")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MonitoredTheoryAttribute : TheoryAttribute
{
    public MonitoredTheoryAttribute()
    {
    }

    public MonitoredTheoryAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}