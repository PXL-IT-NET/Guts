using System.Reflection;
using Guts.Client.Core.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Guts.Client.XUnit.Utility;

internal class XUnitTestClassInfo : ITestClassInfo
{
    public string Name { get; }
    public DirectoryInfo TestProjectDirectory { get; }
    public int NumberOfTests { get; }

    private XUnitTestClassInfo(string name, DirectoryInfo testProjectDirectory, int numberOfTests)
    {
        Name = name;
        TestProjectDirectory = testProjectDirectory;
        NumberOfTests = numberOfTests;
    }

    public static XUnitTestClassInfo CreateFromTestMethod(ITestMethod testMethod)
    {
        var testClassRuntimeType = testMethod.TestClass.Class.ToRuntimeType();

        DirectoryInfo testProjectDirectoryInfo = new DirectoryInfo(testClassRuntimeType.Assembly.Location).Parent!;
        bool hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
        while (!hasProjectFile && testProjectDirectoryInfo.Parent != null)
        {
            testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
            hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
        }

        int numberOfTestsInCurrentClass = testClassRuntimeType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Count(m => m.GetCustomAttributes(inherit: true).Any(a => a is FactAttribute));

        return new XUnitTestClassInfo(testClassRuntimeType.Name, testProjectDirectoryInfo, numberOfTestsInCurrentClass);
    }
}
