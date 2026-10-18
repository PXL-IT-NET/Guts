using System;
using Guts.Infrastructure.Logging;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Logging;

[TestFixture]
internal class FileLoggerOptionsTests
{
    [Test]
    public void Properties_ShouldSetValidValues_AndThrowOnInvalidValues()
    {
        var options = new FileLoggerOptions
        {
            FileSizeLimit = 5,
            RetainedFileCountLimit = 3,
            FileName = "app-",
            Extension = "txt",
            Periodicity = PeriodicityOptions.Hourly,
            LogDirectory = "Logs"
        };

        Assert.That(options.FileSizeLimit, Is.EqualTo(5));
        Assert.That(options.RetainedFileCountLimit, Is.EqualTo(3));
        Assert.That(options.FileName, Is.EqualTo("app-"));
        Assert.That(options.Extension, Is.EqualTo("txt"));
        Assert.That(options.Periodicity, Is.EqualTo(PeriodicityOptions.Hourly));
        Assert.That(options.LogDirectory, Is.EqualTo("Logs"));

        Assert.That(() => options.FileSizeLimit = 0, Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.RetainedFileCountLimit = 0, Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.FileName = "", Throws.InstanceOf<ArgumentException>());
    }
}

