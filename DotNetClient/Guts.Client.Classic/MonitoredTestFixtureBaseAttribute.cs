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

        protected bool AllTestsOfFixtureWereRunned()
        {
            var results = TestRunResultAccumulator.Instance.TestResults;

            TestContext.Progress.WriteLine(
                $"You runned {results.Count} tests " +
                $"of {TestRunResultAccumulator.Instance.NumberOfTestsInCurrentFixture} tests " +
                $"in the test class '{TestRunResultAccumulator.Instance.TestClassName}'");

            if (results.Count >= TestRunResultAccumulator.Instance.NumberOfTestsInCurrentFixture) return true;

            TestContext.Progress.WriteLine("Not all tests of the test class (fixture) were runned. " +
                                           "So the results will not be sent to the GUTS Api. " +
                                           "Run all the tests of a fixture at once to send the results.");
            return false;
        }

        protected string GetSourceCode()
        {
            if (string.IsNullOrEmpty(_sourceCodeRelativeFilePaths)) return string.Empty;

            TestContext.Progress.WriteLine($"Reading source code files: {_sourceCodeRelativeFilePaths}");
            return SourceCodeRetriever.ReadSourceCodeFiles(_sourceCodeRelativeFilePaths);
        }

        protected void SendTestResults(TestRunBase testRun)
        {
            try
            {
                TestContext.Progress.WriteLine("Trying to send results...");

                var result = _resultSender.SendAsync(testRun).Result;

                if (result.Success)
                {
                    TestContext.Progress.WriteLine("Results succesfully sent.");
                }
                else
                {
                    TestContext.Progress.WriteLine("Sending results failed.");
                    TestContext.Progress.WriteLine(result.Message);
                }
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