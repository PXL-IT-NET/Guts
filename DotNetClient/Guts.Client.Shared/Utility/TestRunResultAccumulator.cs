using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Guts.Client.Shared.Models;
using NUnit.Framework;

namespace Guts.Client.Shared.Utility
{
    public class TestRunResultAccumulator
    {
        private static TestRunResultAccumulator _instance;

        public IList<TestResult> TestResults { get; }

        public int NumberOfTestsInCurrentFixture { get; set; }

        public string TestClassName { get; set; }

        private TestRunResultAccumulator()
        {
            TestResults = new List<TestResult>();
        }

        public static TestRunResultAccumulator Instance => _instance ?? (_instance = new TestRunResultAccumulator());

        public void AddTestResult(TestResult result)
        {
            EnsureNumberOfTestsInCurrentFixtureIsRetrieved();

            if (TestResults.Any(r => r.TestName == result.TestName)) return; //avoid duplicated (repeated) tests

            TestResults.Add(result);
        }

        public void Clear()
        {
            TestResults.Clear();
            NumberOfTestsInCurrentFixture = 0;
            TestClassName = string.Empty;
        }

        private void EnsureNumberOfTestsInCurrentFixtureIsRetrieved()
        {
            if (NumberOfTestsInCurrentFixture > 0) return;

            var directoryInfo = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); // .../TestProject/bin/Debug
            var testProjectDirectoryInfo = directoryInfo.Parent.Parent;
            var testAssemblyName = testProjectDirectoryInfo.Name;

            var fullQualifiedTestClassName = $"{TestContext.CurrentContext.Test.ClassName}, {testAssemblyName}";
            var testClassType = Type.GetType(fullQualifiedTestClassName, false);

            if (testClassType != null)
            {
                TestClassName = TestContext.CurrentContext.Test.ClassName;
                NumberOfTestsInCurrentFixture = testClassType.GetMethods().Count(m => m.GetCustomAttributes().OfType<TestAttribute>().Any());
            }
        }
    }
}
