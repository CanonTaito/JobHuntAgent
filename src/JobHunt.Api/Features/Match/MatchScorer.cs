namespace JobHunt.Api.Features.Match;

public sealed class MatchScorer
{
    private static readonly string[] Signals = ["skills", "seniority", "fit", "similarity"];

    private readonly MatchAgent _agent;
    private readonly EmbeddingScorer _embeddings;

    public MatchScorer(MatchAgent agent, EmbeddingScorer embeddings)
    {
        _agent = agent;
        _embeddings = embeddings;
    }

    public async Task<MatchReport> ScoreAsync(string jobDescription, CancellationToken ct)
    {
        var score = await _agent.ScoreAsync(jobDescription, ct);
        var similarity = await _embeddings.ScoreAsync(jobDescription, ct);

        var overall = (int)Math.Round((score.Skills + score.Seniority + score.Fit + similarity) / (double)Signals.Length);

        return new MatchReport
        {
            Overall = overall,
            Similarity = similarity,
            Skills = score.Skills,
            Seniority = score.Seniority,
            Fit = score.Fit,
            Summary = score.Summary,
            Strengths = score.Strengths,
            Gaps = score.Gaps,
        };
    }
}
