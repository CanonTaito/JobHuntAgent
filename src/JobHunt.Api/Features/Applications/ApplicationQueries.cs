using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Applications;

public sealed class ApplicationQueries
{
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;
    private readonly OutcomeInsightsService _insights;
    private readonly MatchReRanker _reRanker;

    public ApplicationQueries(
        IDbContextFactory<JobHuntDbContext> contexts,
        OutcomeInsightsService insights,
        MatchReRanker reRanker)
    {
        _contexts = contexts;
        _insights = insights;
        _reRanker = reRanker;
    }

    public async Task<IReadOnlyList<ApplicationView>> ListAsync(JobApplicationStatus? status, CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();

        var query = db.Applications.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var applications = await query.ToListAsync(ct);
        var insights = await _insights.BuildAsync(ct);

        return applications
            .Select(application => Rank(ApplicationView.From(application), application, insights))
            .OrderByDescending(view => view.RankScore ?? 0)
            .ThenByDescending(view => view.DiscoveredAt)
            .ToList();
    }

    public async Task<ApplicationView?> GetAsync(int applicationId, CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application is null)
        {
            return null;
        }

        var insights = await _insights.BuildAsync(ct);
        return Rank(ApplicationView.From(application), application, insights);
    }

    private ApplicationView Rank(ApplicationView view, JobApplication application, OutcomeInsights insights)
    {
        var ranking = _reRanker.Apply(
            application.OverallScore, application.SalaryBand, application.WorkModel, insights);

        return view with { RankScore = ranking.Score, RankReason = ranking.Reason };
    }
}
