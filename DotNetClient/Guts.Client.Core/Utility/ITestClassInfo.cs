namespace Guts.Client.Core.Utility;

public interface ITestClassInfo
{
    string Name { get; }
    DirectoryInfo TestProjectDirectory { get; }
    int NumberOfTests { get; }
}