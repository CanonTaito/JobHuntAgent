namespace JobHunt.Api.Features.Match;

public sealed record MatchScore
{
    public int Skills { get; init; }

    public int Seniority { get; init; }

    public int Fit { get; init; }

    public int Overall => (Skills + Seniority + Fit + 1) / 3;

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = [];

    public IReadOnlyList<string> Gaps { get; init; } = [];
}
