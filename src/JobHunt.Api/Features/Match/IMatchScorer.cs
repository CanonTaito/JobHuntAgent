namespace JobHunt.Api.Features.Match;

public interface IMatchScorer
{
    Task<MatchReport> ScoreAsync(string jobDescription, CancellationToken ct);
}
