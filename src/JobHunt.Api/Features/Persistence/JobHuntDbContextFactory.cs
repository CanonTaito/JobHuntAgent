using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobHunt.Api.Features.Persistence;

public sealed class JobHuntDbContextFactory : IDesignTimeDbContextFactory<JobHuntDbContext>
{
    public JobHuntDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobHuntDbContext>()
            .UseSqlite("Data Source=jobhunt.db")
            .Options;

        return new JobHuntDbContext(options);
    }
}
