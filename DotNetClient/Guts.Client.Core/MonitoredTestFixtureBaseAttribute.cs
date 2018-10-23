using System;
using System.IO;
using System.Reflection;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.Utility;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
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

            //var executingFolder = new DirectoryInfo(AppContext.BaseDirectory);
            //var projectFolder = executingFolder.Parent?.Parent?.Parent;
            //Assert.That(projectFolder, Is.Not.Null, () => "Technical error: could not find the path of the project.");


            //.SetBasePath(projectFolder.FullName)
            var gutsConfig = new ConfigurationBuilder().AddJsonFile("gutssettings.json", optional: false).Build();
            var gutsSection = gutsConfig.GetSection("Guts");

            string apiBaseUrl = gutsSection.GetValue("apiBaseUrl", string.Empty);
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new Exception("Could not find 'apiBaseUrl' setting in 'gutssettings.json'.");
            }

            string webAppBaseUrl = gutsSection.GetValue("webAppBaseUrl", string.Empty);
            if (string.IsNullOrEmpty(webAppBaseUrl))
            {
                throw new Exception("Could not find 'webAppBaseUrl' setting in 'gutssettings.json'.");
            }

            var httpHandler = new HttpClientToHttpHandlerAdapter(apiBaseUrl);

            var authorizationHandler = new AuthorizationHandler(new LoginWindowFactory(apiBaseUrl, webAppBaseUrl));
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