using System.Runtime.InteropServices.ComTypes;

namespace Guts.Client.Core.Utility;

public interface ITestClassInfo
{
    string Name { get; }
    DirectoryInfo TestProjectDirectory { get; }
    Type Type { get; }
    int NumberOfTests { get; }
}