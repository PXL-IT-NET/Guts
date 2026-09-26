using Xunit;

namespace Guts.Client.XUnit;

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