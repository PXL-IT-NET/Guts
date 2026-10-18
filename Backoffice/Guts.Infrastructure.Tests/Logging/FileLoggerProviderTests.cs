using System;
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
            FlushPeriod = TimeSpan.FromMilliseconds(50),
            Periodicity = periodicity,
            IncludeScopes = true,
            IsEnabled = true,
            RetainedFileCountLimit = 1,
            FileSizeLimit = 10
        };

        var optionsMonitorMock = new Mock<IOptionsMonitor<FileLoggerOptions>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(fileOptions);

        using (var provider = new FileLoggerProvider(optionsMonitorMock.Object))
        {
            ((ISupportExternalScope)provider).SetScopeProvider(new LoggerExternalScopeProvider());
            var logger = provider.CreateLogger("TestCategory");

            using (logger.BeginScope("RequestScope"))
            {
                logger.LogInformation(new EventId(1), new InvalidOperationException("Boom"), "Hello world");
            }

            await Task.Delay(150);
        }

        Assert.That(Directory.Exists(_tempDir), Is.True);
        var files = Directory.GetFiles(_tempDir, "guts-*.log");
        Assert.That(files, Has.Length.EqualTo(1));

        var content = await File.ReadAllTextAsync(files[0]);
        Assert.That(content, Does.Contain("TestCategory"));
        Assert.That(content, Does.Contain("RequestScope"));
        Assert.That(content, Does.Contain("Hello world"));
        Assert.That(content, Does.Contain("Boom"));
    }
}

