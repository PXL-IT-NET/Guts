using Guts.Client.Core.Utility;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestOutputWriter : ITestOutputWriter
{
    private static XUnitTestOutputWriter? _instance;

    public static ITestOutputWriter Instance => _instance ??= new XUnitTestOutputWriter();

    public void WriteError(string error)
    {
        Console.Error.WriteLine(error);
    }

    public void WriteError(Exception exception)
    {
        Console.Error.WriteLine(exception);
    }

    public void WriteProgress(string message)
    {
        Console.WriteLine(message);
    }
}
