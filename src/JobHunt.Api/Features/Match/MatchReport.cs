namespace JobHunt.Api.Features.Match;

public sealed record MatchReport
{
    public int Overall { get; init; }

    public int Similarity { get; init; }

    public int Skills { get; init; }

    public int Seniority { get; init; }

    public int Fit { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = [];

    public IReadOnlyList<string> Gaps { get; init; } = [];
}
