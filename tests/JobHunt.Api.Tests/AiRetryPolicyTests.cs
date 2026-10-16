using System.ClientModel;
using JobHunt.Api.Ai.Retry;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class AiRetryPolicyTests
{
    private static AiRetryPolicy Policy(Action<TimeSpan>? onDelay = null, int maxAttempts = 3) =>
        new(
            new AiRetryOptions { MaxAttempts = maxAttempts, BaseDelayMs = 100, MaxDelayMs = 1000 },
            NullLogger.Instance,
            (delay, _) =>
            {
                onDelay?.Invoke(delay);
                return Task.CompletedTask;
            });

    [Fact]
    public async Task Returns_the_result_without_retrying_when_the_call_succeeds()
    {
        var calls = 0;

        var result = await Policy().ExecuteAsync("chat", _ =>
        {
            calls++;
            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Retries_transient_failures_until_they_succeed()
    {
        var calls = 0;
        var delays = new List<TimeSpan>();

        var result = await Policy(delays.Add).ExecuteAsync("chat", _ =>
        {
            calls++;
            return calls < 3
                ? Task.FromException<int>(new HttpRequestException("boom"))
                : Task.FromResult(7);
        }, CancellationToken.None);

        Assert.Equal(7, result);
        Assert.Equal(3, calls);
        Assert.Equal<TimeSpan>([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)], delays);
    }

    [Fact]
    public async Task Gives_up_after_the_configured_attempts_and_rethrows_the_last_failure()
    {
        var calls = 0;

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => Policy(maxAttempts: 2).ExecuteAsync<int>("chat", _ =>
        {
            calls++;
            return Task.FromException<int>(new HttpRequestException($"boom {calls}"));
        }, CancellationToken.None));

        Assert.Equal(2, calls);
        Assert.Equal("boom 2", exception.Message);
    }

    [Fact]
    public async Task Does_not_retry_permanent_failures()
    {
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Policy().ExecuteAsync<int>("chat", _ =>
        {
            calls++;
            return Task.FromException<int>(new InvalidOperationException("nope"));
        }, CancellationToken.None));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Does_not_retry_caller_cancellation()
    {
        var calls = 0;
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Policy().ExecuteAsync<int>("chat", token =>
        {
            calls++;
            return Task.FromException<int>(new OperationCanceledException(token));
        }, source.Token));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Backs_off_exponentially_and_caps_at_the_maximum()
    {
        var delays = new List<TimeSpan>();

        await Assert.ThrowsAsync<HttpRequestException>(() => Policy(delays.Add, maxAttempts: 7).ExecuteAsync<int>(
            "chat",
            _ => Task.FromException<int>(new HttpRequestException("boom")),
            CancellationToken.None));

        Assert.Equal<TimeSpan>(
            [
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(800),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
            ],
            delays);
    }

    [Fact]
    public async Task Honours_the_retry_after_hint()
    {
        var delays = new List<TimeSpan>();
        var exception = new ClientResultException(new FakePipelineResponse(429, ("Retry-After", "1")));

        await Assert.ThrowsAsync<ClientResultException>(() => Policy(delays.Add, maxAttempts: 2).ExecuteAsync<int>(
            "chat",
            _ => Task.FromException<int>(exception),
            CancellationToken.None));

        Assert.Equal([TimeSpan.FromSeconds(1)], delays);
    }

    [Fact]
    public async Task Caps_the_retry_after_hint_at_the_maximum()
    {
        var delays = new List<TimeSpan>();
        var exception = new ClientResultException(new FakePipelineResponse(429, ("Retry-After", "30")));

        await Assert.ThrowsAsync<ClientResultException>(() => Policy(delays.Add, maxAttempts: 2).ExecuteAsync<int>(
            "chat",
            _ => Task.FromException<int>(exception),
            CancellationToken.None));

        Assert.Equal([TimeSpan.FromMilliseconds(1000)], delays);
    }
}
