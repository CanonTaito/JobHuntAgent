using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class EmbeddingScorerTests
{
    private static readonly CandidateProfile Profile = new() { Name = "Test", Skills = ["alpha"] };

    [Fact]
    public async Task Identical_directions_score_one_hundred()
    {
        var scorer = Build(_ => [1f, 0f]);

        var score = await scorer.ScoreAsync("any text", CancellationToken.None);

        Assert.Equal(100, score);
    }

    [Fact]
    public async Task Orthogonal_directions_score_zero()
    {
        var scorer = Build(text => text.Contains("alpha") ? [1f, 0f] : [0f, 1f]);

        var score = await scorer.ScoreAsync("unrelated job", CancellationToken.None);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task Partial_overlap_scores_the_cosine_similarity()
    {
        var scorer = Build(text => text.Contains("alpha") ? [1f, 0f] : [0.6f, 0.8f]);

        var score = await scorer.ScoreAsync("unrelated job", CancellationToken.None);

        Assert.Equal(60, score);
    }

    private static EmbeddingScorer Build(Func<string, float[]> vector)
    {
        var redactor = new PiiRedactor(Profile, new InMemoryPiiMappingStore(), NullLogger<PiiRedactor>.Instance);
        return new EmbeddingScorer(new FakeEmbeddingGenerator(vector), redactor, Profile);
    }
}
