using System;
using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
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
            SourceCodeRelativeFilePaths = null;
        }

        public ProjectComponentTestFixtureAttribute(string courseCode, string projectCode, string componentCode, string sourceCodeRelativeFilePaths) : this(courseCode, projectCode, componentCode)
        {
            SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
        }

        public override void BeforeTest(ITest test)
        {
            base.BeforeTest(test);
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
                    CourseCode = CourseCode,
                    TopicCode = _projectCode,
                    AssignmentCode = _componentCode,
                };

                var testRun = new AssignmentTestRun(
                    projectComponent,
                    TestRunResultAccumulator.Instance.TestResults,
                    GetSourceCodeFiles(),
                    TestRunResultAccumulator.Instance.TestCodeHash);

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