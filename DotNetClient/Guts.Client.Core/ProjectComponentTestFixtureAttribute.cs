using System;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProjectComponentTestFixtureAttribute : MonitoredTestFixtureBaseAttribute
    {
        private readonly string _projectCode;
        private readonly string _componentCode;

        public ProjectComponentTestFixtureAttribute(string courseCode, string projectCode, string componentCode) : base(courseCode)
        {
            _projectCode = projectCode;
            _componentCode = componentCode;
        }

        public ProjectComponentTestFixtureAttribute(string courseCode, string projectCode, string componentCode, string sourceCodeRelativeFilePaths) : this(courseCode, projectCode, componentCode)
        {
            _sourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
        }

        public override void BeforeTest(ITest test)
        {
            TestRunResultAccumulator.Instance.Clear();
            TestContext.Progress.WriteLine($"Starting test run. Project '{_projectCode}', component '{_componentCode}'.");
        }

        public override void AfterTest(ITest test)
        {
            try
            {
                if (!AllTestsOfFixtureWereRunned()) return;

                var projectComponent = new Assignment
                {
                    CourseCode = _courseCode,
                    TopicCode = _projectCode,
                    AssignmentCode = _componentCode,
                };

                var testRun = new AssignmentTestRun
                {
                    Assignment = projectComponent,
                    Results = TestRunResultAccumulator.Instance.TestResults,
                    SourceCode = GetSourceCode(),
                    TestCodeHash = TestRunResultAccumulator.Instance.TestCodeHash
                };

                SendTestResults(testRun, TestRunType.ForProject);
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine("Something went wrong while sending the test results.");
                TestContext.Error.WriteLine($"Exception: {ex}");
            }
        }
    }
}