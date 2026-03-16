using System.Net.Mail;
using System.Reflection;
using Guts.Client.Core.Models;
using Guts.Client.Core.TestTools;
using Guts.Client.Core.Utility;
using Guts.Client.XUnit.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Guts.Client.XUnit;

public abstract class MonitoredTestClassBaseAttribute : Attribute
{
    protected readonly string CourseCode;
    protected string? SourceCodeRelativeFilePaths;
    protected readonly TestRunResultSender? ResultSender;

    private readonly Exception? _initializationException;

    protected MonitoredTestClassBaseAttribute(string courseCode)
    {
        SourceCodeRelativeFilePaths = null;
        CourseCode = courseCode;
        ResultSender = null;

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
                                    $"{AppContext.BaseDirectory} and {Assembly.GetEntryAssembly()!.Location}.");
            }

            var provider = new PhysicalFileProvider(gutsSettingsDirectory);
            IConfigurationRoot gutsConfig = new ConfigurationBuilder().AddJsonFile(provider, "gutssettings.json", optional: false, reloadOnChange: false).Build();
            IConfigurationSection gutsSection = gutsConfig.GetSection("Guts");

            string? apiBaseUrl = gutsSection["apiBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new Exception("Could not find 'apiBaseUrl' setting in 'gutssettings.json'.");
            }

            string? webAppBaseUrl = gutsSection["webAppBaseUrl"];
            if (string.IsNullOrEmpty(webAppBaseUrl))
            {
                throw new Exception("Could not find 'webAppBaseUrl' setting in 'gutssettings.json'.");
            }

            var httpHandler = new HttpClientToHttpHandlerAdapter(apiBaseUrl);
            var outputWriter = XUnitTestOutputWriter.Instance;
            var authorizationHandler = new AuthorizationHandler(new LoginWindowFactory(httpHandler, webAppBaseUrl), outputWriter);
            ResultSender = new TestRunResultSender(httpHandler, authorizationHandler, outputWriter);
            
        }
        catch (Exception e)
        {
            _initializationException = e;
        }
    }

    public async Task SendTestResults(ITestClassInfo testClassInfo, IReadOnlyList<TestResult> results)
    {
        if (_initializationException is not null)
        {
            XUnitTestOutputWriter.Instance.WriteError(_initializationException);
            return;
        }

        try
        {
            XUnitTestOutputWriter.Instance.WriteProgress(
                $"{results.Count} of {testClassInfo.NumberOfTests} tests of class '{testClassInfo.Name}' completed. Trying to send results...");

            var testRun = new AssignmentTestRun(
                CreateAssignment(),
                results,
                GetSourceCodeFiles(),
                GetTestClassHash(testClassInfo));

            Result result = await ResultSender!.SendAsync(testRun, RunType);

            if (result.Success)
            {
                XUnitTestOutputWriter.Instance.WriteProgress("Results successfully sent.");
            }
            else
            {
                XUnitTestOutputWriter.Instance.WriteProgress("Sending results failed.");
                XUnitTestOutputWriter.Instance.WriteProgress(result.Message);
            }
        }
        catch (AggregateException ex)
        {
            XUnitTestOutputWriter.Instance.WriteError("Something went wrong while sending the test results.");
            foreach (var innerException in ex.InnerExceptions)
            {
                XUnitTestOutputWriter.Instance.WriteError(innerException);
            }
        }
        catch (Exception ex)
        {
            XUnitTestOutputWriter.Instance.WriteError("Something went wrong while sending the test results.");
            XUnitTestOutputWriter.Instance.WriteError(ex);
        }
    }

    protected abstract Assignment CreateAssignment();

    protected abstract TestRunType RunType { get; }

    private IEnumerable<SolutionFile> GetSourceCodeFiles()
    {
        if (string.IsNullOrEmpty(SourceCodeRelativeFilePaths)) return new List<SolutionFile>();

        XUnitTestOutputWriter.Instance.WriteProgress($"Reading source code files: {SourceCodeRelativeFilePaths}");
        return SourceCodeRetriever.ReadSourceCodeFiles(SourceCodeRelativeFilePaths);
    }

    private string GetSettingsFileDirectory(string baseDirectory)
    {
        const string relativeFilePath = "gutssettings.json";
        DirectoryInfo testProjectDirectoryInfo = new DirectoryInfo(baseDirectory);
        FileInfo fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));
        while (!fileInfo.Exists && testProjectDirectoryInfo.Parent != null)
        {
            testProjectDirectoryInfo = testProjectDirectoryInfo.Parent;
            fileInfo = new FileInfo(Path.Combine(testProjectDirectoryInfo.FullName, relativeFilePath));

            if (testProjectDirectoryInfo.Name.Equals("packages", StringComparison.OrdinalIgnoreCase))
            {
                var nugetDirectory = testProjectDirectoryInfo
                    .EnumerateDirectories("guts.client.core", SearchOption.TopDirectoryOnly).FirstOrDefault();
                DirectoryInfo? lastVersionDirectory = nugetDirectory?.EnumerateDirectories()
                    .OrderByDescending(di => di.Name).FirstOrDefault();
                if (lastVersionDirectory is not null)
                {
                    fileInfo = lastVersionDirectory.EnumerateFiles(relativeFilePath, SearchOption.AllDirectories).First();
                    testProjectDirectoryInfo = fileInfo.Directory!;
                }
            }
        }

        return fileInfo.Exists ? testProjectDirectoryInfo.FullName : string.Empty;
    }

    private string GetTestClassHash(ITestClassInfo testClassInfo)
    {
        var testCodeHash = string.Empty;

        FileInfo? fileInfo = testClassInfo.TestProjectDirectory
            .GetFiles(testClassInfo.Name + ".cs", SearchOption.AllDirectories).FirstOrDefault();

        if (fileInfo is null)
        {
            XUnitTestOutputWriter.Instance.WriteError(
                $"Could not find test code file for test class {testClassInfo.Name}. " +
                $"Searched for {testClassInfo.Name}.cs in directory {testClassInfo.TestProjectDirectory.FullName} (and subdirectories)");
        }
        else
        {
            testCodeHash = FileUtil.GetFileHash(fileInfo.FullName);
        }
        return testCodeHash;
    }
}
