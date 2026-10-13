using JobHunt.Api.Features.Discovery;
using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Salary;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class JobDiscoveryPipelineTests
{
    [Fact]
    public async Task Scores_persists_and_reports_each_new_listing()
    {
        var source = new FilteringJobSource(
        [
            (Listing("job-1", 150_000, 185_000), 150_000, 185_000),
            (Listing("job-2", 130_000, 160_000), 130_000, 160_000),
        ]);
        using var dbFactory = new TestDbContextFactory();
        var pipeline = BuildPipeline(source, dbFactory, overall: 80);

        var report = await pipeline.RunAsync(10, CancellationToken.None);

        Assert.Equal(2, report.Discovered);
        Assert.Equal(2, report.Scored);
        Assert.Equal(0, report.Skipped);
        Assert.Equal("150-199k", report.Items[0].SalaryBand);
        Assert.Equal("120-149k", report.Items[1].SalaryBand);
        Assert.All(report.Items, item => Assert.Equal("Scored", item.Status));

        await using var db = dbFactory.CreateDbContext();
        Assert.Equal(2, await db.Applications.CountAsync());
        Assert.Equal(80, (await db.Applications.FirstAsync()).OverallScore);
    }

    [Fact]
    public async Task Skips_listings_that_were_already_discovered()
    {
        var source = new FilteringJobSource([(Listing("job-1", 150_000, 185_000), 150_000, 185_000)]);
        using var dbFactory = new TestDbContextFactory();
        var pipeline = BuildPipeline(source, dbFactory, overall: 80);

        await pipeline.RunAsync(10, CancellationToken.None);
        var second = await pipeline.RunAsync(10, CancellationToken.None);

        Assert.Equal(1, second.Discovered);
        Assert.Equal(0, second.Scored);
        Assert.Equal(1, second.Skipped);
        Assert.Empty(second.Items);

        await using var db = dbFactory.CreateDbContext();
        Assert.Equal(1, await db.Applications.CountAsync());
    }

    private static JobDiscoveryPipeline BuildPipeline(FilteringJobSource source, TestDbContextFactory dbFactory, int overall) =>
        new(
            source,
            new StubMatchScorer(overall),
            new SalaryBandInference(source),
            new CandidateProfile(),
            dbFactory,
            new JobHuntMetrics(new TestMeterFactory()),
            NullLogger<JobDiscoveryPipeline>.Instance);

    private static JobListing Listing(string id, int? min, int? max) => new()
    {
        Id = id,
        Source = "fake",
        Title = "Engineer",
        Company = "Acme",
        Description = "Engineer role",
        SalaryMin = min,
        SalaryMax = max,
    };

    private sealed class StubMatchScorer(int overall) : IMatchScorer
    {
        public Task<MatchReport> ScoreAsync(string jobDescription, CancellationToken ct) =>
            Task.FromResult(new MatchReport
            {
                Overall = overall,
                Similarity = overall,
                Skills = overall,
                Seniority = overall,
                Fit = overall,
                Summary = "ok",
            });
    }
}
