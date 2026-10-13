using JobHunt.Api.Features.Observability;

namespace JobHunt.Api.Features.Match;

public sealed class MatchScorer : IMatchScorer
{
    private static readonly string[] Signals = ["skills", "seniority", "fit", "similarity"];

    private readonly IMatchAgent _agent;
    private readonly EmbeddingScorer _embeddings;
    private readonly JobHuntMetrics _metrics;

    public MatchScorer(IMatchAgent agent, EmbeddingScorer embeddings, JobHuntMetrics metrics)
    {
        _agent = agent;
        _embeddings = embeddings;
        _metrics = metrics;
    }

    public async Task<MatchReport> ScoreAsync(string jobDescription, CancellationToken ct)
    {
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("match.score");

        var score = await _agent.ScoreAsync(jobDescription, ct);
        var similarity = await _embeddings.ScoreAsync(jobDescription, ct);

        var overall = (int)Math.Round((score.Skills + score.Seniority + score.Fit + similarity) / (double)Signals.Length);

        activity?.SetTag("match.overall", overall);
        activity?.SetTag("match.similarity", similarity);
        activity?.SetTag("match.skills", score.Skills);
        activity?.SetTag("match.seniority", score.Seniority);
        activity?.SetTag("match.fit", score.Fit);
        _metrics.RecordMatchScore(overall);

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
