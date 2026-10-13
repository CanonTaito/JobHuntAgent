namespace JobHunt.Api.Features.Ranking;

public sealed record OutcomeInsights
{
    public int ApplicationsWithOutcomes { get; init; }

    public int PositiveApplications { get; init; }

    public IReadOnlyDictionary<string, OutcomeStat> Bands { get; init; } =
        new Dictionary<string, OutcomeStat>();

    public IReadOnlyDictionary<string, OutcomeStat> WorkModels { get; init; } =
        new Dictionary<string, OutcomeStat>();

    public double OverallRate =>
        ApplicationsWithOutcomes == 0 ? 0d : (double)PositiveApplications / ApplicationsWithOutcomes;

    public static OutcomeInsights Empty { get; } = new();
}

public sealed record OutcomeStat(int Positive, int Total)
{
    public double Rate => Total == 0 ? 0d : (double)Positive / Total;
}
