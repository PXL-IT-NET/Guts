using Xunit;

namespace Guts.Client.XUnit;

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