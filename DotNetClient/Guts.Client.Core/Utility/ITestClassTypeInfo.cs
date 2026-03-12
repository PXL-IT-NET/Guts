namespace Guts.Client.Core.Utility;

public interface ITestClassTypeInfo
{
    string Name { get; }

    string AssemblyLocation { get; }

    int NumberOfTestsInCurrentFixture { get; }
}
