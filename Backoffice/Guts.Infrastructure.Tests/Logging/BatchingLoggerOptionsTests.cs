using System;
using Guts.Infrastructure.Logging;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Logging;

[TestFixture]
internal class BatchingLoggerOptionsTests
{
    [Test]
    public void Properties_ShouldSetValidValues_AndThrowOnInvalidValues()
    {
        var options = new BatchingLoggerOptions
        {
            FlushPeriod = TimeSpan.FromMilliseconds(500),
            BackgroundQueueSize = 100,
            BatchSize = 10,
            IsEnabled = true,
            IncludeScopes = true
        };

        Assert.That(options.FlushPeriod, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
        Assert.That(options.BackgroundQueueSize, Is.EqualTo(100));
        Assert.That(options.BatchSize, Is.EqualTo(10));
        Assert.That(options.IsEnabled, Is.True);
        Assert.That(options.IncludeScopes, Is.True);

        Assert.That(() => options.FlushPeriod = TimeSpan.Zero, Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.BackgroundQueueSize = -1, Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.BatchSize = 0, Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}

