using System.ClientModel;
using System.Diagnostics;
using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class FallbackEmbeddingGeneratorTests
{
    private static AiEmbeddingAttempt Attempt(string providerName, int keyIndex, FakeEmbeddingGenerator generator) =>
        new(providerName, keyIndex, generator);

    private static FallbackEmbeddingGenerator CreateGenerator(params AiEmbeddingAttempt[] attempts) =>
        new(attempts, NullLogger<FallbackEmbeddingGenerator>.Instance);

    [Fact]
    public async Task Returns_the_first_successful_embeddings()
    {
        var first = new FakeEmbeddingGenerator(_ => [1f, 0f]);
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f]);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("gemini", 0, second));

        var result = await generator.GenerateAsync(["profile"]);

        Assert.Equal([1f, 0f], result[0].Vector.ToArray());
        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task Advances_to_the_next_key_after_a_transient_failure()
    {
        var first = new FakeEmbeddingGenerator(_ => [1f, 0f], new HttpRequestException("refused"));
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f]);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("ollama", 1, second));

        var result = await generator.GenerateAsync(["profile"]);

        Assert.Equal([0f, 1f], result[0].Vector.ToArray());
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Falls_back_to_a_different_provider_after_an_authentication_failure()
    {
        var first = new FakeEmbeddingGenerator(_ => [1f, 0f], new ClientResultException(new FakePipelineResponse(401)));
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f]);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("gemini", 0, second));

        var result = await generator.GenerateAsync(["profile"]);

        Assert.Equal([0f, 1f], result[0].Vector.ToArray());
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Does_not_advance_after_a_permanent_failure()
    {
        var first = new FakeEmbeddingGenerator(_ => [1f, 0f], new ClientResultException(new FakePipelineResponse(400)));
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f]);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("gemini", 0, second));

        await Assert.ThrowsAsync<ClientResultException>(() => generator.GenerateAsync(["profile"]));

        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task Throws_the_last_failure_when_every_attempt_fails()
    {
        var last = new HttpRequestException("refused again");
        var first = new FakeEmbeddingGenerator(_ => [1f, 0f], new HttpRequestException("refused"));
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f], last);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("gemini", 0, second));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => generator.GenerateAsync(["profile"]));

        Assert.Same(last, exception);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Does_not_advance_when_the_caller_cancels()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var first = new FakeEmbeddingGenerator(_ => [1f, 0f], new OperationCanceledException(cancellation.Token));
        var second = new FakeEmbeddingGenerator(_ => [0f, 1f]);
        var generator = CreateGenerator(Attempt("ollama", 0, first), Attempt("gemini", 0, second));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync(["profile"], cancellationToken: cancellation.Token));

        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public void Requires_at_least_one_attempt()
    {
        Assert.Throws<ArgumentException>(() => CreateGenerator());
    }

    [Fact]
    public async Task Records_a_provider_fallback_span_event()
    {
        using var listener = CreateListener();
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("test")
            ?? throw new InvalidOperationException("The test activity was not sampled.");

        var first = Attempt("ollama", 0, new FakeEmbeddingGenerator(_ => [1f, 0f], new HttpRequestException("refused")));
        var second = Attempt("gemini", 0, new FakeEmbeddingGenerator(_ => [0f, 1f]));
        var generator = CreateGenerator(first, second);

        var result = await generator.GenerateAsync(["profile"]);
        Assert.Equal([0f, 1f], result[0].Vector.ToArray());

        var fallback = activity.Events.Single(e => e.Name == "provider.fallback");
        var tags = fallback.Tags.ToDictionary(tag => tag.Key, tag => tag.Value?.ToString() ?? string.Empty);

        Assert.Equal("ollama", tags["provider"]);
        Assert.Equal("gemini", tags["next.provider"]);
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == JobHuntTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        return listener;
    }
}
