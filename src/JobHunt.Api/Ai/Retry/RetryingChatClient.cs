using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Retry;

public sealed class RetryingChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly AiRetryPolicy _policy;
    private readonly string? _target;

    public RetryingChatClient(IChatClient innerClient, AiRetryPolicy policy, string? target = null)
    {
        _innerClient = innerClient;
        _policy = policy;
        _target = target;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialized = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        return _policy.ExecuteAsync(
            "chat",
            token => _innerClient.GetResponseAsync(materialized, options, token),
            cancellationToken,
            _target);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        _innerClient.GetService(serviceType, serviceKey);

    public void Dispose() => _innerClient.Dispose();
}
