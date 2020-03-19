﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
{
    public abstract class MonitoredTestFixtureBaseAttribute : TestFixtureAttribute, ITestAction
    {
        protected readonly string _courseCode;
        protected string _sourceCodeRelativeFilePaths;
        protected readonly TestRunResultSender _resultSender;
        private Exception _initializationException;

        public ActionTargets Targets => ActionTargets.Suite;

        protected MonitoredTestFixtureBaseAttribute(string courseCode) : base(new object[0])
        {
            _courseCode = courseCode;

            try
            {
                var gutsSettingsDirectory = GetSettingsFileDirectory(AppContext.BaseDirectory);
                if (string.IsNullOrEmpty(gutsSettingsDirectory))
                {
                    gutsSettingsDirectory = GetSettingsFileDirectory(Assembly.GetCallingAssembly().Location);
                }
                if (string.IsNullOrEmpty(gutsSettingsDirectory))
                {
                    throw new Exception("Could not find 'gutssettings.json' Searched in the following directories (and upper directories): " +
                                        $"{AppContext.BaseDirectory} and {Assembly.GetEntryAssembly().Location}.");
                }

                var provider = new PhysicalFileProvider(gutsSettingsDirectory);
                var gutsConfig = new ConfigurationBuilder().AddJsonFile(provider,"gutssettings.json", optional: false, reloadOnChange: false).Build();
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

                var authorizationHandler = new AuthorizationHandler(new LoginWindowFactory(httpHandler, webAppBaseUrl));
                _resultSender = new TestRunResultSender(httpHandler, authorizationHandler);
            }
            catch (Exception e)
            {
                _initializationException = e;
            }
        }

        public virtual void BeforeTest(ITest test)
        {
            if (_initializationException != null)
            {
                throw _initializationException;
            }
        }

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

        protected IEnumerable<SolutionFile> GetSourceCodeFiles()
        {
            if (string.IsNullOrEmpty(_sourceCodeRelativeFilePaths)) return new List<SolutionFile>();

            TestContext.Progress.WriteLine($"Reading source code files: {_sourceCodeRelativeFilePaths}");
            return SourceCodeRetriever.ReadSourceCodeFiles(_sourceCodeRelativeFilePaths);
        }

        protected void SendTestResults(AssignmentTestRun testRun, TestRunType type)
        {
            try
            {
                TestContext.Progress.WriteLine("Test run completed. Trying to send results...");

                var result = _resultSender.SendAsync(testRun, type).Result;

                if (result.Success)
                {
                    TestContext.Progress.WriteLine("Results successfully sent.");
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

        private string GetSettingsFileDirectory(string baseDirectory)
        {
            var relativeFilePath = "gutssettings.json";
            var testProjectDirectoryInfo = new DirectoryInfo(baseDirectory);
            var fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));
            while (!fileInfo.Exists && testProjectDirectoryInfo.Parent != null)
            {
                testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
                fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));

                //Hack: get json file in global nuget directory
                if (testProjectDirectoryInfo.Name.ToLower() == "packages")
                {
                    var nugetDirectory = testProjectDirectoryInfo
                        .EnumerateDirectories("guts.client.core", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    var lastVersionDirectory = nugetDirectory?.EnumerateDirectories()
                        .OrderByDescending(di => di.Name).FirstOrDefault();
                    if (lastVersionDirectory != null)
                    {
                        fileInfo = lastVersionDirectory.EnumerateFiles(relativeFilePath, SearchOption.AllDirectories).FirstOrDefault();
                        if (fileInfo != null)
                        {
                            testProjectDirectoryInfo = fileInfo.Directory;
                        }
                    }
                }
            }
            return fileInfo.Exists ? testProjectDirectoryInfo.FullName : string.Empty;
        }
    }
}