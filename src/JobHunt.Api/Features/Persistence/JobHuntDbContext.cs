using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Outcomes;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Persistence;

public sealed class JobHuntDbContext : DbContext
{
    public JobHuntDbContext(DbContextOptions<JobHuntDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobApplication> Applications => Set<JobApplication>();

    public DbSet<OutcomeRecord> Outcomes => Set<OutcomeRecord>();

    public DbSet<PiiMappingRecord> PiiMappings => Set<PiiMappingRecord>();

    public DbSet<ProfileRecord> Profiles => Set<ProfileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var applications = modelBuilder.Entity<JobApplication>();
        applications.HasIndex(a => new { a.Source, a.SourceId }).IsUnique();
        applications.Property(a => a.Status).HasConversion<string>();
        applications.Property(a => a.SalarySource).HasConversion<string>();

        var outcomes = modelBuilder.Entity<OutcomeRecord>();
        outcomes.HasIndex(o => o.JobApplicationId);
        outcomes.Property(o => o.Outcome).HasConversion<string>();
        outcomes
            .HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(o => o.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        var piiMappings = modelBuilder.Entity<PiiMappingRecord>();
        piiMappings.HasIndex(m => m.Value).IsUnique();
        piiMappings.HasIndex(m => m.Placeholder).IsUnique();

        var profiles = modelBuilder.Entity<ProfileRecord>();
        profiles.Property(p => p.Json).IsRequired();
    }
}
