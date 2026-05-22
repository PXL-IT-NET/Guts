using Guts.Client.Core.Utility;
using System.Diagnostics;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestOutputWriter : ITestOutputWriter
{
    private static XUnitTestOutputWriter? _instance;

    public static ITestOutputWriter Instance => _instance ??= new XUnitTestOutputWriter();

    public void WriteError(string error)
    {
        TestContext.Current.TestOutputHelper?.Write("Error - ");
        TestContext.Current.TestOutputHelper?.WriteLine(error);
        Debug.WriteLine(error);
        Console.Error.WriteLine(error);
    }

    public void WriteError(Exception exception)
    {
        TestContext.Current.TestOutputHelper?.Write("Error - ");
        TestContext.Current.TestOutputHelper?.WriteLine(exception.ToString());
        Debug.WriteLine(exception);
        Console.Error.WriteLine(exception);
    }

    public void WriteProgress(string message)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(message);
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
}
