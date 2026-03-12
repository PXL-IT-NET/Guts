namespace Guts.Client.Core.Utility;

public interface ITestOutputWriter
{
    void WriteError(string error);
    void WriteError(Exception exception);
    void WriteProgress(string message);
}