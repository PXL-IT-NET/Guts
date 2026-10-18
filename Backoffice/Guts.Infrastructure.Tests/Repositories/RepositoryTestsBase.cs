using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

internal abstract class RepositoryTestsBase
{
    private SqliteConnection _connection;

    protected TestDataSeeder Seeder { get; private set; }

    [SetUp]
    public void BaseSetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        Seeder = new TestDataSeeder(CreateDbContext);
    }

    [TearDown]
    public void BaseTearDown()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    protected GutsContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GutsContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        return new GutsContext(options);
    }
}
