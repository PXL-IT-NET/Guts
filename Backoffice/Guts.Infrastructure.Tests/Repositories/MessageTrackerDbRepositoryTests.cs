using System;
using System.Threading.Tasks;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class MessageTrackerDbRepositoryTests : RepositoryTestsBase
{
    private DateTime _nowUtc;

    [SetUp]
    public void SetUp()
    {
        _nowUtc = DateTime.UtcNow;
    }

    [Test]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveByCompositeKey()
    {
        var email = new Email("student@pxl.be");
        var tracker = new MessageTrackerFactory().CreateNew("PeerAssessment", 42, email, _nowUtc);

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var added = await repo.AddAsync(tracker);
            Assert.That(added, Is.Not.Null);
            Assert.That(added.EntityType, Is.EqualTo("PeerAssessment"));
            Assert.That(added.EntityId, Is.EqualTo(42));
            Assert.That(added.Email, Is.EqualTo(email));
        }

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var retrieved = await repo.GetByIdAsync("PeerAssessment", 42, email);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Status, Is.EqualTo(MessageStatus.Sending));
            Assert.That(retrieved.RetryCount, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task FindByEntityAndEmailAsync_WhenExists_ShouldReturnTracker()
    {
        var email = new Email("existing@pxl.be");
        var tracker = new MessageTrackerFactory().CreateNew("PeerAssessment", 10, email, _nowUtc);

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            await repo.AddAsync(tracker);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var found = await repo.FindByEntityAndEmailAsync("PeerAssessment", 10, email);
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Email, Is.EqualTo(email));
        }
    }

    [Test]
    public async Task FindByEntityAndEmailAsync_WhenDoesNotExist_ShouldReturnNull()
    {
        await using var context = CreateDbContext();
        var repo = new MessageTrackerDbRepository(context);

        var found = await repo.FindByEntityAndEmailAsync("PeerAssessment", 999, new Email("nobody@pxl.be"));
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task FindByEntityAsync_ShouldReturnAllTrackersForEntity()
    {
        var email1 = new Email("peer1@pxl.be");
        var email2 = new Email("peer2@pxl.be");
        var emailOther = new Email("other@pxl.be");

        var factory = new MessageTrackerFactory();
        var tracker1 = factory.CreateNew("PeerAssessment", 15, email1, _nowUtc);
        var tracker2 = factory.CreateNew("PeerAssessment", 15, email2, _nowUtc);
        var trackerOther = factory.CreateNew("PeerAssessment", 99, emailOther, _nowUtc);

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            await repo.AddAsync(tracker1);
            await repo.AddAsync(tracker2);
            await repo.AddAsync(trackerOther);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var results = await repo.FindByEntityAsync("PeerAssessment", 15);
            Assert.That(results, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistStatusAndFailureReason()
    {
        var email = new Email("update@pxl.be");
        var tracker = new MessageTrackerFactory().CreateNew("PeerAssessment", 20, email, _nowUtc);

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            await repo.AddAsync(tracker);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var toUpdate = await repo.FindByEntityAndEmailAsync("PeerAssessment", 20, email);
            toUpdate.MarkFailed("SMTP connection timeout");
            await repo.UpdateAsync(toUpdate);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new MessageTrackerDbRepository(context);
            var updated = await repo.FindByEntityAndEmailAsync("PeerAssessment", 20, email);
            Assert.That(updated.Status, Is.EqualTo(MessageStatus.Failed));
            Assert.That(updated.FailureReason, Is.EqualTo("SMTP connection timeout"));
            Assert.That(updated.RetryCount, Is.EqualTo(1));
        }
    }
}

