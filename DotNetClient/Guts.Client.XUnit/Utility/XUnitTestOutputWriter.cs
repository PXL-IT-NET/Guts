using Guts.Client.Core.Utility;
using System.Diagnostics;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestOutputWriter : ITestOutputWriter
{
    private static XUnitTestOutputWriter? _instance;

    public static ITestOutputWriter Instance => _instance ??= new XUnitTestOutputWriter();

    public void WriteError(string error)
    {
        Debug.WriteLine(error);
        Console.Error.WriteLine(error);
    }

    public void WriteError(Exception exception)
    {
        Debug.WriteLine(exception);
        Console.Error.WriteLine(exception);
    }

    public void WriteProgress(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
}
