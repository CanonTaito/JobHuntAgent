using JobHunt.Api.Features.Outcomes;
using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Ranking;

public sealed class OutcomeInsightsService
{
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public OutcomeInsightsService(IDbContextFactory<JobHuntDbContext> contexts) => _contexts = contexts;

    public async Task<OutcomeInsights> BuildAsync(CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();

        var outcomes = await db.Outcomes
            .AsNoTracking()
            .Select(o => new { o.JobApplicationId, o.Outcome })
            .ToListAsync(ct);

        if (outcomes.Count == 0)
        {
            return OutcomeInsights.Empty;
        }

        var applications = await db.Applications
            .AsNoTracking()
            .Select(a => new { a.Id, a.SalaryBand, a.WorkModel })
            .ToListAsync(ct);
        var byId = applications.ToDictionary(a => a.Id);

        var positive = outcomes
            .Where(o => o.Outcome is ApplicationOutcome.Interview or ApplicationOutcome.Offer)
            .Select(o => o.JobApplicationId)
            .ToHashSet();

        var bands = new Dictionary<string, OutcomeStat>(StringComparer.OrdinalIgnoreCase);
        var workModels = new Dictionary<string, OutcomeStat>(StringComparer.OrdinalIgnoreCase);
        var counted = new HashSet<int>();
        var positiveCount = 0;

        foreach (var applicationId in outcomes.Select(o => o.JobApplicationId).Distinct())
        {
            if (!byId.TryGetValue(applicationId, out var application))
            {
                continue;
            }

            var isPositive = positive.Contains(applicationId);
            counted.Add(applicationId);
            if (isPositive)
            {
                positiveCount++;
            }

            Tally(bands, application.SalaryBand, isPositive);
            Tally(workModels, application.WorkModel, isPositive);
        }

        return new OutcomeInsights
        {
            ApplicationsWithOutcomes = counted.Count,
            PositiveApplications = positiveCount,
            Bands = bands,
            WorkModels = workModels,
        };
    }

    private static void Tally(Dictionary<string, OutcomeStat> stats, string? key, bool positive)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        var current = stats.TryGetValue(key, out var stat) ? stat : new OutcomeStat(0, 0);
        stats[key] = new OutcomeStat(current.Positive + (positive ? 1 : 0), current.Total + 1);
    }
}
