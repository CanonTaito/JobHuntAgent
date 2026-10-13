namespace JobHunt.Api.Features.Ranking;

public sealed class MatchReRanker
{
    public const int MinimumApplications = 4;
    public const int MinimumBucketSamples = 2;
    public const int MaxAdjustment = 15;

    private const double FeatureWeight = 25d;

    public RankingResult Apply(
        int? overallScore,
        string? salaryBand,
        string? workModel,
        OutcomeInsights insights)
    {
        if (overallScore is null || insights.ApplicationsWithOutcomes < MinimumApplications)
        {
            return new RankingResult(overallScore, null);
        }

        var baseScore = overallScore.Value;
        var overallRate = insights.OverallRate;
        var delta = 0d;
        var reasons = new List<string>();

        if (TryBucket(insights.Bands, salaryBand, out var band))
        {
            delta += (band.Rate - overallRate) * FeatureWeight;
            reasons.Add($"band {salaryBand} replies {Percent(band.Rate)} (overall {Percent(overallRate)})");
        }

        if (TryBucket(insights.WorkModels, workModel, out var model))
        {
            delta += (model.Rate - overallRate) * FeatureWeight;
            reasons.Add($"{workModel} roles reply {Percent(model.Rate)} (overall {Percent(overallRate)})");
        }

        var adjustment = Math.Clamp((int)Math.Round(delta), -MaxAdjustment, MaxAdjustment);
        if (adjustment == 0 || reasons.Count == 0)
        {
            return new RankingResult(baseScore, null);
        }

        var score = Math.Clamp(baseScore + adjustment, 0, 100);
        var verb = adjustment > 0 ? "boosted" : "penalised";
        return new RankingResult(score, $"{verb} {adjustment.ToString("+0;-0")} — {string.Join("; ", reasons)}");
    }

    private static bool TryBucket(
        IReadOnlyDictionary<string, OutcomeStat> stats,
        string? key,
        out OutcomeStat stat)
    {
        if (!string.IsNullOrEmpty(key) &&
            stats.TryGetValue(key, out var found) &&
            found.Total >= MinimumBucketSamples)
        {
            stat = found;
            return true;
        }

        stat = null!;
        return false;
    }

    private static string Percent(double rate) => $"{(int)Math.Round(rate * 100)}%";
}
