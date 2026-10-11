using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Salary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Discovery;

public sealed class JobDiscoveryPipeline
{
    private readonly IJobSource _source;
    private readonly MatchScorer _scorer;
    private readonly SalaryBandInference _salaryInference;
    private readonly CandidateProfile _profile;
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;
    private readonly ILogger<JobDiscoveryPipeline> _logger;

    public JobDiscoveryPipeline(
        IJobSource source,
        MatchScorer scorer,
        SalaryBandInference salaryInference,
        CandidateProfile profile,
        IDbContextFactory<JobHuntDbContext> contexts,
        ILogger<JobDiscoveryPipeline> logger)
    {
        _source = source;
        _scorer = scorer;
        _salaryInference = salaryInference;
        _profile = profile;
        _contexts = contexts;
        _logger = logger;
    }

    public async Task<DiscoveryReport> RunAsync(int maxResults, CancellationToken ct)
    {
        var query = BuildQuery(maxResults);
        var listings = await _source.SearchAsync(query, ct);

        await using var db = _contexts.CreateDbContext();
        var seen = (await db.Applications
                .AsNoTracking()
                .Select(application => new { application.Source, application.SourceId })
                .ToListAsync(ct))
            .Select(application => (application.Source, application.SourceId))
            .ToHashSet();

        var items = new List<DiscoveryItem>();
        var skipped = 0;

        foreach (var listing in listings)
        {
            if (seen.Contains((listing.Source, listing.Id)))
            {
                skipped++;
                continue;
            }

            var report = await _scorer.ScoreAsync(listing.Description, ct);
            var salary = await _salaryInference.InferAsync(listing, query, ct);
            var now = DateTimeOffset.UtcNow;

            var application = new JobApplication
            {
                SourceId = listing.Id,
                Source = listing.Source,
                Title = listing.Title,
                Company = listing.Company,
                Location = listing.Location,
                WorkModel = listing.WorkModel,
                Url = listing.Url,
                Description = listing.Description,
                SalaryMin = listing.SalaryMin,
                SalaryMax = listing.SalaryMax,
                SalaryCurrency = listing.SalaryCurrency,
                SalaryBand = salary.Band?.Label,
                SalarySource = salary.Source,
                Status = JobApplicationStatus.Scored,
                OverallScore = report.Overall,
                SimilarityScore = report.Similarity,
                SkillsScore = report.Skills,
                SeniorityScore = report.Seniority,
                FitScore = report.Fit,
                MatchSummary = report.Summary,
                PostedAt = listing.PostedAt,
                DiscoveredAt = now,
                UpdatedAt = now,
            };

            db.Applications.Add(application);
            await db.SaveChangesAsync(ct);
            seen.Add((listing.Source, listing.Id));

            items.Add(new DiscoveryItem
            {
                ApplicationId = application.Id,
                SourceId = listing.Id,
                Title = listing.Title,
                Company = listing.Company,
                Overall = report.Overall,
                SalaryBand = application.SalaryBand,
                Status = application.Status.ToString(),
            });
        }

        _logger.LogInformation(
            "Discovery via {Source}: {Discovered} discovered, {Scored} scored, {Skipped} skipped",
            _source.Name, listings.Count, items.Count, skipped);

        return new DiscoveryReport
        {
            Discovered = listings.Count,
            Scored = items.Count,
            Skipped = skipped,
            Items = items,
        };
    }

    private JobSearchQuery BuildQuery(int maxResults) => new()
    {
        Keywords = _profile.Preferences.Roles,
        Locations = _profile.Preferences.Locations,
        WorkModels = _profile.Preferences.WorkModels,
        MaxResults = maxResults,
    };
}
