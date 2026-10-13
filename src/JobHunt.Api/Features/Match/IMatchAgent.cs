namespace JobHunt.Api.Features.Match;

public interface IMatchAgent
{
    Task<MatchScore> ScoreAsync(string jobDescription, CancellationToken ct);
}
