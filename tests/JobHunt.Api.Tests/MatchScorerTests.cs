using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class MatchScorerTests
{
    [Fact]
    public async Task Overall_is_the_mean_of_skills_seniority_fit_and_similarity()
    {
        var profile = new CandidateProfile { Name = "Test", Skills = ["alpha"] };
        var redactor = new PiiRedactor(profile, new InMemoryPiiMappingStore(), NullLogger<PiiRedactor>.Instance);
        var embeddings = new EmbeddingScorer(
            new FakeEmbeddingGenerator(text => text.Contains("alpha") ? [1f, 0f] : [0.6f, 0.8f]),
            redactor,
            profile);

        var scorer = new MatchScorer(
            new StubMatchAgent(new MatchScore { Skills = 90, Seniority = 80, Fit = 70 }),
            embeddings,
            new JobHuntMetrics(new TestMeterFactory()));

        var report = await scorer.ScoreAsync("unrelated job", CancellationToken.None);

        Assert.Equal(60, report.Similarity);
        Assert.Equal(90, report.Skills);
        Assert.Equal(80, report.Seniority);
        Assert.Equal(70, report.Fit);
        Assert.Equal(75, report.Overall);
    }

    private sealed class StubMatchAgent(MatchScore score) : IMatchAgent
    {
        public Task<MatchScore> ScoreAsync(string jobDescription, CancellationToken ct) =>
            Task.FromResult(score);
    }
}
