using System;
using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExerciseTestFixtureAttribute : MonitoredTestFixtureBaseAttribute
    {
        private readonly string _chapterCode;
        private readonly string _exerciseCode;

        public ExerciseTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode) : base(courseCode)
        {
            _chapterCode = chapterCode;
            _exerciseCode = exerciseCode;
            SourceCodeRelativeFilePaths = null;
        }

        public ExerciseTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode, string sourceCodeRelativeFilePaths) : this(courseCode, chapterCode, exerciseCode)
        {
            SourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
        }

        public override void BeforeTest(ITest test)
        {
            base.BeforeTest(test);
            TestRunResultAccumulator.Instance.Clear();
            TestContext.Progress.WriteLine($"Starting test run. Chapter {_chapterCode}, exercise {_exerciseCode}");
        }

        public override void AfterTest(ITest test)
        {
            TestContext.Progress.WriteLine("Test run completed.");

            try
            {
                if (!AllTestsOfFixtureWereRunned()) return;

                var exercise = new Assignment
                {
                    CourseCode = CourseCode,
                    TopicCode = _chapterCode,
                    AssignmentCode = _exerciseCode
                };

                var testRun = new AssignmentTestRun(
                    exercise, 
                    TestRunResultAccumulator.Instance.TestResults,
                    GetSourceCodeFiles(), 
                    TestRunResultAccumulator.Instance.TestCodeHash);


               SendTestResults(testRun, TestRunType.ForExercise);
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine("Something went wrong while sending the test results.");
                TestContext.Error.WriteLine($"Exception: {ex}");
            }
        }
    }
}
