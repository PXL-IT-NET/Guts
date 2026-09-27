using Guts.Client.Core.Utility;
using System.Reflection;
using Xunit;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestClassInfo : ITestClassInfo
{
    public string Name { get; }
    public DirectoryInfo TestProjectDirectory { get; }
    public Type Type { get; }
    public int NumberOfTests { get; }

    private XUnitTestClassInfo(string name, Type type, DirectoryInfo testProjectDirectory, int numberOfTests)
    {
        Name = name;
        Type = type;
        TestProjectDirectory = testProjectDirectory;
        NumberOfTests = numberOfTests;
    }

    public static XUnitTestClassInfo CreateFromType(Type testClassRuntimeType)
    {
        string baseDir = string.IsNullOrEmpty(testClassRuntimeType.Assembly.Location)
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(testClassRuntimeType.Assembly.Location) ?? AppContext.BaseDirectory;

        DirectoryInfo testProjectDirectoryInfo = new(baseDir);
        bool hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Length != 0;
        while (!hasProjectFile && testProjectDirectoryInfo.Parent != null)
        {
            testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
            hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Length != 0;
        }

        int numberOfTestsInCurrentClass = testClassRuntimeType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Sum(m => m.GetCustomAttributes(inherit: true).Count(a => a is FactAttribute and not TheoryAttribute || a is InlineDataAttribute));

        return new XUnitTestClassInfo(testClassRuntimeType.Name, testClassRuntimeType, testProjectDirectoryInfo, numberOfTestsInCurrentClass);
    }
}
