using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Pii;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Persistence;

public sealed class JobHuntDbContext : DbContext
{
    public JobHuntDbContext(DbContextOptions<JobHuntDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobApplication> Applications => Set<JobApplication>();

    public DbSet<PiiMappingRecord> PiiMappings => Set<PiiMappingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var applications = modelBuilder.Entity<JobApplication>();
        applications.HasIndex(a => new { a.Source, a.SourceId }).IsUnique();
        applications.Property(a => a.Status).HasConversion<string>();
        applications.Property(a => a.SalarySource).HasConversion<string>();

        var piiMappings = modelBuilder.Entity<PiiMappingRecord>();
        piiMappings.HasIndex(m => m.Value).IsUnique();
        piiMappings.HasIndex(m => m.Placeholder).IsUnique();
    }
}
