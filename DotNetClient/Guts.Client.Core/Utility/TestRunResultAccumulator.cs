using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Guts.Client.Core.Models;
using Guts.Client.Core.TestTools;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core.Utility
{
    public class TestRunResultAccumulator
    {
        private static TestRunResultAccumulator _instance;

        public IList<TestResult> TestResults { get; }

        public int NumberOfTestsInCurrentFixture { get; set; }

        public string TestClassName { get; set; }

        public string TestCodeHash { get; set; }

        private TestRunResultAccumulator()
        {
            TestResults = new List<TestResult>();
            Clear();
        }

        public static TestRunResultAccumulator Instance => _instance ?? (_instance = new TestRunResultAccumulator());

        public void AddTestResult(TestResult result, ITypeInfo testClassTypeInfo)
        {
            EnsureMetaDataIsLoaded(testClassTypeInfo);

            if (TestResults.Any(r => r.TestName == result.TestName)) return; //avoid duplicated (repeated) tests

            TestResults.Add(result);
        }

        public void Clear()
        {
            TestResults.Clear();
            NumberOfTestsInCurrentFixture = 0;
            TestClassName = string.Empty;
            TestCodeHash = string.Empty;
        }

        private void EnsureMetaDataIsLoaded(ITypeInfo testClassTypeInfo)
        {
            if (NumberOfTestsInCurrentFixture > 0) return;

            if (testClassTypeInfo == null) return;

            TestClassName = testClassTypeInfo.Name;

            //Find test project directory
            DirectoryInfo testProjectDirectoryInfo = new DirectoryInfo(testClassTypeInfo.Assembly.Location).Parent;
            bool hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
            while (!hasProjectFile && testProjectDirectoryInfo.Parent != null)
            {
                testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
                hasProjectFile = testProjectDirectoryInfo.GetFiles("*.csproj").Any();
            }

            //Find test code file in test project directory
            FileInfo fileInfo = testProjectDirectoryInfo
                .GetFiles(TestClassName + ".cs", SearchOption.AllDirectories).FirstOrDefault();

            if (fileInfo == null)
            {
                TestContext.Error.WriteLine(
                    $"Could not find test code file for test class {TestClassName}. " +
                    $"Searched for {TestClassName}.cs in directory {testProjectDirectoryInfo.FullName} (and subdirectories)");
            }

            NumberOfTestsInCurrentFixture = testClassTypeInfo
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes<TestAttribute>(inherit: true).Any())
                .Select(m => Math.Max(1, m.GetCustomAttributes<TestAttribute>(inherit: true).Count())).Sum();

            TestCodeHash = FileUtil.GetFileHash(fileInfo.FullName);
        }
    }
}
