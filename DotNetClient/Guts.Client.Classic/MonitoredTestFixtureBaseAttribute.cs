using System;
using System.Configuration;
using Guts.Client.Classic.UI;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Classic
{
    public abstract class MonitoredTestFixtureBaseAttribute : TestFixtureAttribute, ITestAction
    {
        protected readonly string _courseCode;
        protected string _sourceCodeRelativeFilePaths;
        protected readonly TestRunResultSender _resultSender;

        public ActionTargets Targets => ActionTargets.Suite;

        protected MonitoredTestFixtureBaseAttribute(string courseCode)
        {
            _courseCode = courseCode;

            string apiBaseUrl = ConfigurationManager.AppSettings["GutsApiUri"];
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new ConfigurationErrorsException("Could not find an appsetting 'GutsApiUri' that contains a valid Api url.");
            }

            var httpHandler = new HttpClientToHttpHandlerAdapter(apiBaseUrl);

            var authorizationHandler = new AuthorizationHandler(new LoginWindowFactory(httpHandler));
            _resultSender = new TestRunResultSender(httpHandler, authorizationHandler);
        }

        protected MonitoredTestFixtureBaseAttribute(string courseCode, string sourceCodeRelativeFilePaths) : this(courseCode)
        {
            _sourceCodeRelativeFilePaths = sourceCodeRelativeFilePaths;
        }

        public abstract void BeforeTest(ITest test);

        public abstract void AfterTest(ITest test);

        protected void SendTestResults(TestRunBase testRun)
        {
            try
            {
                TestContext.Progress.WriteLine("Test run completed. Trying to send results...");

                var success = _resultSender.SendAsync(testRun).Result;

                TestContext.Progress.WriteLine(success ? "Results succesfully sent." : "Sending results failed.");
            }
            catch (AggregateException ex)
            {
                TestContext.Error.WriteLine("Something went wrong while sending the test results.");
                foreach (var innerException in ex.InnerExceptions)
                {
                    TestContext.Error.WriteLine($"Exception: {innerException}");
                }
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine("Something went wrong while sending the test results.");
                TestContext.Error.WriteLine($"Exception: {ex}");
            }
        }
    }
}