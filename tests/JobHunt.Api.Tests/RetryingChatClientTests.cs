using JobHunt.Api.Ai.Retry;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class RetryingChatClientTests
{
    private static RetryingChatClient Client(IChatClient inner) =>
        new(
            inner,
            new AiRetryPolicy(
                new AiRetryOptions { MaxAttempts = 3, BaseDelayMs = 1, MaxDelayMs = 5 },
                NullLogger.Instance,
                (_, _) => Task.CompletedTask));

    [Fact]
    public async Task Retries_a_transient_failure_and_returns_the_response()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "pong"));
        var inner = new FakeChatClient(response, new HttpRequestException("boom"));

        var result = await Client(inner).GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]);

        Assert.Equal(2, inner.Calls);
        Assert.Same(response, result);
    }

    [Fact]
    public async Task Surfaces_the_last_failure_when_retries_are_exhausted()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "pong"));
        var inner = new FakeChatClient(
            response,
            new HttpRequestException("boom 1"),
            new HttpRequestException("boom 2"),
            new HttpRequestException("boom 3"));

        await Assert.ThrowsAsync<HttpRequestException>(() => Client(inner).GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));

        Assert.Equal(3, inner.Calls);
    }
}
