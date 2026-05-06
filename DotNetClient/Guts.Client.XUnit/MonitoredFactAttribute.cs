using Xunit;
using Xunit.v3;

namespace Guts.Client.XUnit;

[XunitTestCaseDiscoverer(typeof(Utility.MonitoredFactDiscoverer))]
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