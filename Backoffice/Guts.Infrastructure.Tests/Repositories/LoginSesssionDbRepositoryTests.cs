using System;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Domain.LoginSessionAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class LoginSesssionDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetSingleAsync_ShouldFilterByPublicIdentifierIpAndToken()
    {
        await using (var context = CreateDbContext())
        {
            var repo = new LoginSesssionDbRepository(context);
            await repo.AddAsync(new LoginSession
            {
                PublicIdentifier = "pub-123",
                IpAddress = "127.0.0.1",
                SessionToken = "tok-abc",
                LoginToken = "log-xyz",
                CreateDateTime = DateTime.UtcNow
            });
        }

        await using (var context = CreateDbContext())
        {
            var repo = new LoginSesssionDbRepository(context);

            var byIdOnly = await repo.GetSingleAsync("pub-123");
            Assert.That(byIdOnly.LoginToken, Is.EqualTo("log-xyz"));

            var byIdAndIp = await repo.GetSingleAsync("pub-123", "127.0.0.1");
            Assert.That(byIdAndIp.SessionToken, Is.EqualTo("tok-abc"));

            var byAll = await repo.GetSingleAsync("pub-123", "127.0.0.1", "tok-abc");
            Assert.That(byAll.Id, Is.GreaterThan(0));

            Assert.That(async () => await repo.GetSingleAsync("pub-123", "10.0.0.1"),
                Throws.InstanceOf<DataNotFoundException>());
            Assert.That(async () => await repo.GetSingleAsync("pub-123", "127.0.0.1", "wrong-token"),
                Throws.InstanceOf<DataNotFoundException>());
        }
    }
}
