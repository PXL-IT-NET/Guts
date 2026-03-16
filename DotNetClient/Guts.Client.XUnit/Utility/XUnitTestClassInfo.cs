using Guts.Client.Core.TestTools;
using Guts.Client.Core.Utility;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

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
            .Sum(m => m.GetCustomAttributes(inherit: true).Count(a => a is FactAttribute and not TheoryAttribute || a is InlineDataAttribute));

        return new XUnitTestClassInfo(testClassRuntimeType.Name, testClassRuntimeType, testProjectDirectoryInfo, numberOfTestsInCurrentClass);
    }
}
