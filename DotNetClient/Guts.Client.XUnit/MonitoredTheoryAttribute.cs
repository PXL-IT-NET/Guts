using Xunit;
using Xunit.v3;

namespace Guts.Client.XUnit;

[XunitTestCaseDiscoverer(typeof(Utility.MonitoredTheoryDiscoverer))]
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