using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.TestTools;
using NUnit.Framework;

namespace Guts.Client.Shared.Utility
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

        public void AddTestResult(TestResult result)
        {
            EnsureMetaDataIsLoaded();

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

        private void EnsureMetaDataIsLoaded()
        {
            if (NumberOfTestsInCurrentFixture > 0) return;


            var testClassName = TestContext.CurrentContext.Test.ClassName;
            var dotIndex = testClassName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                testClassName = testClassName.Substring(dotIndex + 1);
            }
            var relativeFilePath = testClassName + ".cs";
            var testProjectDirectoryInfo = new DirectoryInfo(AppContext.BaseDirectory);
            var fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));
            while (!fileInfo.Exists && testProjectDirectoryInfo.Parent != null)
            {
                testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
                fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));
            }
            var testAssemblyName = testProjectDirectoryInfo.Name;


            var fullQualifiedTestClassName = $"{TestContext.CurrentContext.Test.ClassName}, {testAssemblyName}";
            var testClassType = Type.GetType(fullQualifiedTestClassName, false);

            if (testClassType != null)
            {
                TestClassName = TestContext.CurrentContext.Test.ClassName;
                NumberOfTestsInCurrentFixture = testClassType.GetMethods().Count(m => m.GetCustomAttributes().OfType<TestAttribute>().Any());

                var testCodePath = Path.Combine(testProjectDirectoryInfo.FullName, testClassType.Name + ".cs");
                TestCodeHash = FileUtil.GetFileHash(testCodePath);
            }
        }
    }
}
