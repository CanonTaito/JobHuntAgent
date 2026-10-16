using System.ClientModel;
using JobHunt.Api.Ai.Retry;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class RetryingEmbeddingGeneratorTests
{
    private static RetryingEmbeddingGenerator Generator(FakeEmbeddingGenerator inner) =>
        new(
            inner,
            new AiRetryPolicy(
                new AiRetryOptions { MaxAttempts = 3, BaseDelayMs = 1, MaxDelayMs = 5 },
                NullLogger.Instance,
                (_, _) => Task.CompletedTask));

    [Fact]
    public async Task Retries_a_transient_failure_and_returns_the_embeddings()
    {
        var inner = new FakeEmbeddingGenerator(_ => [1f, 0f], new HttpRequestException("boom"));

        var result = await Generator(inner).GenerateAsync(["profile"]);

        Assert.Equal(2, inner.Calls);
        Assert.Equal([1f, 0f], result[0].Vector.ToArray());
    }

    [Fact]
    public async Task Surfaces_the_last_failure_when_retries_are_exhausted()
    {
        var inner = new FakeEmbeddingGenerator(
            _ => [1f, 0f],
            new HttpRequestException("boom 1"),
            new HttpRequestException("boom 2"),
            new HttpRequestException("boom 3"));

        await Assert.ThrowsAsync<HttpRequestException>(() => Generator(inner).GenerateAsync(["profile"]));

        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task Does_not_retry_a_permanent_failure()
    {
        var inner = new FakeEmbeddingGenerator(
            _ => [1f, 0f],
            new ClientResultException(new FakePipelineResponse(400)));

        await Assert.ThrowsAsync<ClientResultException>(() => Generator(inner).GenerateAsync(["profile"]));

        Assert.Equal(1, inner.Calls);
    }
}
