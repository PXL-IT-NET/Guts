using Xunit;
using Xunit.Sdk;

namespace Guts.Client.XUnit;

[XunitTestCaseDiscoverer("Guts.Client.XUnit.Utility.MonitoredFactDiscoverer", "Guts.Client.XUnit")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MonitoredFactAttribute : FactAttribute
{
    public MonitoredFactAttribute()
    {
    }

    public MonitoredFactAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}