using Microsoft.Extensions.AI;

namespace JobHunt.Api.Tests.TestSupport;

internal sealed class ScriptedChatClient : IChatClient
{
    private readonly Queue<string> _replies;

    public ScriptedChatClient(params string[] replies) => _replies = new Queue<string>(replies);

    public int Calls { get; private set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastMessages = messages.ToList();

        if (_replies.Count == 0)
        {
            throw new InvalidOperationException("The scripted chat client has no reply left.");
        }

        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, _replies.Dequeue())]));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
