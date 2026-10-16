using Microsoft.Extensions.AI;

namespace JobHunt.Api.Tests.TestSupport;

internal sealed class FakeChatClient : IChatClient
{
    private readonly ChatResponse _response;
    private readonly Queue<Exception> _failures;

    public FakeChatClient(ChatResponse response, params Exception[] failures)
    {
        _response = response;
        _failures = new Queue<Exception>(failures);
    }

    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return _failures.Count > 0
            ? Task.FromException<ChatResponse>(_failures.Dequeue())
            : Task.FromResult(_response);
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
