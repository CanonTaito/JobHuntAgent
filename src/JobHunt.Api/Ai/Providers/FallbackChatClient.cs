using JobHunt.Api.Ai.Retry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Ai.Providers;

public sealed class FallbackChatClient : IChatClient
{
    private readonly IReadOnlyList<AiChatAttempt> _attempts;
    private readonly ILogger _logger;

    public FallbackChatClient(IReadOnlyList<AiChatAttempt> attempts, ILogger<FallbackChatClient> logger)
    {
        if (attempts.Count == 0)
        {
            throw new ArgumentException("At least one AI attempt is required.", nameof(attempts));
        }

        _attempts = attempts;
        _logger = logger;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialized = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        for (var index = 0; index < _attempts.Count; index++)
        {
            var attempt = _attempts[index];

            try
            {
                return await attempt.Client.GetResponseAsync(materialized, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (index < _attempts.Count - 1 && AiErrorClassifier.ShouldAdvance(exception))
            {
                _logger.LogWarning(
                    exception,
                    "AI provider '{Provider}' key #{KeyIndex} failed; trying the next attempt.",
                    attempt.ProviderName,
                    attempt.KeyIndex);
            }
        }

        throw new InvalidOperationException("Every AI attempt failed.");
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _attempts[0].Client.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        _attempts[0].Client.GetService(serviceType, serviceKey);

    public void Dispose()
    {
        foreach (var attempt in _attempts)
        {
            attempt.Client.Dispose();
        }
    }
}
