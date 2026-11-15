using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.BackgroundServices;
using NUnit.Framework;

namespace Guts.Api.Tests.BackgroundServices;

[TestFixture]
public class InMemoryDeletionJobQueueTests
{
    [Test]
    public async Task EnqueueAndDequeueAll_DeliversEnqueuedJobIdsInOrder()
    {
        var queue = new InMemoryDeletionJobQueue();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await queue.EnqueueAsync(id1);
        await queue.EnqueueAsync(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var enumerator = queue.DequeueAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        Assert.That(await enumerator.MoveNextAsync(), Is.True);
        Assert.That(enumerator.Current, Is.EqualTo(id1));

        Assert.That(await enumerator.MoveNextAsync(), Is.True);
        Assert.That(enumerator.Current, Is.EqualTo(id2));
    }
}
