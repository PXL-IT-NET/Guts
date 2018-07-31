using System;
using System.Configuration;
using Guts.Client.Classic.UI;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Classic
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MonitoredTestFixtureAttribute : TestFixtureAttribute, ITestAction
    {
        private readonly string _courseCode;
        private readonly int _chapter;
        private readonly int _exercise;
        private readonly TestRunResultSender _resultSender;

        public ActionTargets Targets => ActionTargets.Suite;

        public MonitoredTestFixtureAttribute(string courseCode, int chapter, int exercise)
        {
            _courseCode = courseCode;
            _chapter = chapter;
            _exercise = exercise;

            string apiBaseUrl = ConfigurationManager.AppSettings["GutsApiUri"];
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new ConfigurationErrorsException($"Could not find an appsetting 'GutsApiUri' that contains a valid Api url.");
            }

            var httpHandler = new HttpClientToHttpHandlerAdapter(apiBaseUrl);

            var authorizationHandler = new AuthorizationHandler(httpHandler, new LoginWindowFactory());
            _resultSender = new TestRunResultSender(httpHandler, authorizationHandler);
        }

        public void BeforeTest(ITest test)
        {
            TestRunResultAccumulator.Instance.Clear();
            TestContext.Progress.WriteLine($"Starting test run. Chapter {_chapter}, exercise {_exercise}");
        }

        public void AfterTest(ITest test)
        {
            try
            {
                var results = TestRunResultAccumulator.Instance.TestResults;

                var exercise = new Exercise
                {
                    CourseCode = _courseCode,
                    ChapterNumber = _chapter,
                    ExerciseNumber = _exercise,
                };

                var testRun = new TestRun()
                {
                    Exercise = exercise,
                    Results = results
                };

                TestContext.Progress.WriteLine("Test run completed. Trying to send results...");

                var success = _resultSender.SendAsync(testRun).Result;

                TestContext.Progress.WriteLine(success ? "Results succesfully sent." : "Sending results failed.");
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine("Something went wrong while sending the test results.");
                TestContext.Error.WriteLine($"Something went wrong while sending the test results. Exception: {ex}");
            }
        }
    }
}
