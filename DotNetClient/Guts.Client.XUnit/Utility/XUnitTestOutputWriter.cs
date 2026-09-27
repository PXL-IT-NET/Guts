using Guts.Client.Core.Utility;
using System.Diagnostics;
using Xunit;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestOutputWriter : ITestOutputWriter
{
    private static XUnitTestOutputWriter? _instance;

    public static ITestOutputWriter Instance => _instance ??= new XUnitTestOutputWriter();

    public void WriteError(string error)
    {
        WriteToTestOutput($"Error - {error}");
    }

    public void WriteError(Exception exception)
    {
        WriteToTestOutput($"Error - {exception}");
        
    }

    public void WriteProgress(string message)
    {
        WriteToTestOutput(message);
    }

    private static void WriteToTestOutput(string message)
    {
        try
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
            TestContext.Current.SendDiagnosticMessage(message);
        }
        catch
        {
            // Best effort only
        }
    }
}
