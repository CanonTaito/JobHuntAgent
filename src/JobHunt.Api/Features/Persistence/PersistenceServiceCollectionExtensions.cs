using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobHunt.Api.Features.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultConnectionString = "Data Source=jobhunt.db";

    public static IServiceCollection AddJobHuntDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = ResolveConnectionString(configuration, environment);
        services.AddDbContextFactory<JobHuntDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString("JobHunt");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = DefaultConnectionString;
        }

        var builder = new SqliteConnectionStringBuilder(configured);
        if (!string.IsNullOrEmpty(builder.DataSource) &&
            !Path.IsPathRooted(builder.DataSource) &&
            !builder.DataSource.Equals(":memory:", StringComparison.Ordinal))
        {
            builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);
        }

        return builder.ConnectionString;
    }
}
