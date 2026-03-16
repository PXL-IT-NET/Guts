using System.Reflection;
using Guts.Client.Core.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.NUnit.Utility;

internal class NUnitTestFixtureInfo : ITestClassInfo
{
    public string Name { get; }
    public DirectoryInfo TestProjectDirectory { get; }
    public Type Type { get; }
    public int NumberOfTests { get; }

    private NUnitTestFixtureInfo(string name, Type type, DirectoryInfo testProjectDirectory, int numberOfTests)
    {
        Name = name;
        Type = type;
        TestProjectDirectory = testProjectDirectory;
        NumberOfTests = numberOfTests;
    }

    public static NUnitTestFixtureInfo CreateFromTest(ITest test)
    {
        ITypeInfo? testClassTypeInfo = test.Method?.TypeInfo!;
        string testClassName = testClassTypeInfo.Name;

        DirectoryInfo testProjectDirectoryInfo = new DirectoryInfo(testClassTypeInfo.Assembly.Location).Parent!;
        bool hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
        while (!hasProjectFile && testProjectDirectoryInfo.Parent != null)
        {
            testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
            hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
        }

        int numberOfTestsInCurrentFixture = testClassTypeInfo
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes<TestAttribute>(inherit: true).Any())
            .Select(m => Math.Max(1, m.GetCustomAttributes<TestAttribute>(inherit: true).Count())).Sum();

        return new NUnitTestFixtureInfo(testClassName, testClassTypeInfo.Type, testProjectDirectoryInfo, numberOfTestsInCurrentFixture);
    }
}