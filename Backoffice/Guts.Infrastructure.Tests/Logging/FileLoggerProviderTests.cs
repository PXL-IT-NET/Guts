using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Guts.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Logging;

[TestFixture]
internal class FileLoggerProviderTests
{
    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GutsLoggingTests_" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [TestCase(PeriodicityOptions.Minutely)]
    [TestCase(PeriodicityOptions.Hourly)]
    [TestCase(PeriodicityOptions.Daily)]
    [TestCase(PeriodicityOptions.Monthly)]
    public async Task WriteMessagesAsync_ShouldWriteFormattedLogMessagesAndRollFiles(PeriodicityOptions periodicity)
    {
        var fileOptions = new FileLoggerOptions
        {
            LogDirectory = _tempDir,
            FileName = "guts-",
            Extension = "log",
            FlushPeriod = TimeSpan.FromMilliseconds(20),
            Periodicity = periodicity,
            IncludeScopes = true,
            IsEnabled = true,
            RetainedFileCountLimit = 1,
            FileSizeLimit = 10
        };

        var optionsMonitorMock = new Mock<IOptionsMonitor<FileLoggerOptions>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(fileOptions);

        string[] files = [];
        using (var provider = new FileLoggerProvider(optionsMonitorMock.Object))
        {
            ((ISupportExternalScope)provider).SetScopeProvider(new LoggerExternalScopeProvider());
            var logger = provider.CreateLogger("TestCategory");

            using (logger.BeginScope("RequestScope"))
            {
                logger.LogInformation(new EventId(1), new InvalidOperationException("Boom"), "Hello world");
            }

            files = await WaitForFlushedLogFilesAsync(_tempDir, "guts-*.log", TimeSpan.FromSeconds(5));
        }

        Assert.That(Directory.Exists(_tempDir), Is.True);
        Assert.That(files, Has.Length.EqualTo(1));

        var content = await File.ReadAllTextAsync(files[0]);
        Assert.That(content, Does.Contain("TestCategory"));
        Assert.That(content, Does.Contain("RequestScope"));
        Assert.That(content, Does.Contain("Hello world"));
        Assert.That(content, Does.Contain("Boom"));
    }

    private static async Task<string[]> WaitForFlushedLogFilesAsync(string directory, string searchPattern, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, searchPattern);
                if (files.Length == 1 && IsFileFlushedAndClosed(files[0]))
                {
                    return files;
                }
            }

            await Task.Delay(25);
        }

        return Directory.Exists(directory) ? Directory.GetFiles(directory, searchPattern) : [];
    }

    private static bool IsFileFlushedAndClosed(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

