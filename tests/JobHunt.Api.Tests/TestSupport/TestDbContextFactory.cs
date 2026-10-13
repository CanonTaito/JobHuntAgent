using JobHunt.Api.Features.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Tests.TestSupport;

public sealed class TestDbContextFactory : IDbContextFactory<JobHuntDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public JobHuntDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobHuntDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new JobHuntDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
