using System.ClientModel;
using System.Diagnostics;
using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class FallbackChatClientTests
{
    private static readonly ChatResponse Success = new([new ChatMessage(ChatRole.Assistant, "ok")]);

    private static AiChatAttempt Attempt(int keyIndex, FakeChatClient client) =>
        new("gemini", keyIndex, client);

    private static FallbackChatClient CreateClient(params AiChatAttempt[] attempts) =>
        new(attempts, NullLogger<FallbackChatClient>.Instance);

    [Fact]
    public async Task Returns_the_first_successful_response()
    {
        var first = new FakeChatClient(Success);
        var second = new FakeChatClient(Success);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Same(Success, response);
        Assert.Equal(1, first.Calls);
        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task Advances_to_the_next_key_after_a_transient_failure()
    {
        var first = new FakeChatClient(Success, new HttpRequestException("refused"));
        var second = new FakeChatClient(Success);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Same(Success, response);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Advances_to_the_next_key_after_an_authentication_failure()
    {
        var first = new FakeChatClient(Success, new ClientResultException(new FakePipelineResponse(401)));
        var second = new FakeChatClient(Success);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Same(Success, response);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Does_not_advance_after_a_permanent_failure()
    {
        var first = new FakeChatClient(Success, new ClientResultException(new FakePipelineResponse(400)));
        var second = new FakeChatClient(Success);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        await Assert.ThrowsAsync<ClientResultException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));

        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task Throws_the_last_failure_when_every_attempt_fails()
    {
        var last = new HttpRequestException("refused again");
        var first = new FakeChatClient(Success, new HttpRequestException("refused"));
        var second = new FakeChatClient(Success, last);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));

        Assert.Same(last, exception);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Does_not_advance_when_the_caller_cancels()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var first = new FakeChatClient(Success, new OperationCanceledException(cancellation.Token));
        var second = new FakeChatClient(Success);
        var client = CreateClient(Attempt(0, first), Attempt(1, second));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: cancellation.Token));

        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public void Requires_at_least_one_attempt()
    {
        Assert.Throws<ArgumentException>(() => CreateClient());
    }

    [Fact]
    public async Task Falls_back_to_a_different_provider_after_a_failure()
    {
        var firstClient = new FakeChatClient(Success, new HttpRequestException("refused"));
        var secondClient = new FakeChatClient(Success);
        var client = new FallbackChatClient(
            [new AiChatAttempt("gemini", 0, firstClient), new AiChatAttempt("groq", 0, secondClient)],
            NullLogger<FallbackChatClient>.Instance);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Same(Success, response);
        Assert.Equal(1, firstClient.Calls);
        Assert.Equal(1, secondClient.Calls);
    }

    [Fact]
    public async Task Records_a_provider_fallback_span_event()
    {
        using var listener = CreateListener();
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("test")
            ?? throw new InvalidOperationException("The test activity was not sampled.");

        var first = new AiChatAttempt("gemini", 0, new FakeChatClient(Success, new HttpRequestException("refused")));
        var second = new AiChatAttempt("groq", 0, new FakeChatClient(Success));
        var client = new FallbackChatClient([first, second], NullLogger<FallbackChatClient>.Instance);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);
        Assert.Same(Success, response);

        var fallback = activity.Events.Single(e => e.Name == "provider.fallback");
        var tags = fallback.Tags.ToDictionary(tag => tag.Key, tag => tag.Value?.ToString() ?? string.Empty);

        Assert.Equal("gemini", tags["provider"]);
        Assert.Equal("groq", tags["next.provider"]);
    }

    [Fact]
    public async Task Does_not_record_a_fallback_event_for_key_rotation_within_one_provider()
    {
        using var listener = CreateListener();
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("test")
            ?? throw new InvalidOperationException("The test activity was not sampled.");

        var first = new AiChatAttempt("gemini", 0, new FakeChatClient(Success, new HttpRequestException("refused")));
        var second = new AiChatAttempt("gemini", 1, new FakeChatClient(Success));
        var client = new FallbackChatClient([first, second], NullLogger<FallbackChatClient>.Instance);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Same(Success, response);
        Assert.DoesNotContain(activity.Events, e => e.Name == "provider.fallback");
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
