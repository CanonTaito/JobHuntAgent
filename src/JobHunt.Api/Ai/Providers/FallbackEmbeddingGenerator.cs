using System.Diagnostics;
using JobHunt.Api.Ai.Retry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Ai.Providers;

public sealed class FallbackEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IReadOnlyList<AiEmbeddingAttempt> _attempts;
    private readonly ILogger _logger;

    public FallbackEmbeddingGenerator(IReadOnlyList<AiEmbeddingAttempt> attempts, ILogger<FallbackEmbeddingGenerator> logger)
    {
        if (attempts.Count == 0)
        {
            throw new ArgumentException("At least one embedding attempt is required.", nameof(attempts));
        }

        _attempts = attempts;
        _logger = logger;
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialized = values as IReadOnlyList<string> ?? values.ToList();

        for (var index = 0; index < _attempts.Count; index++)
        {
            var attempt = _attempts[index];

            try
            {
                return await attempt.Generator.GenerateAsync(materialized, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (index < _attempts.Count - 1 && AiErrorClassifier.ShouldAdvance(exception))
            {
                var next = _attempts[index + 1];

                if (string.Equals(next.ProviderName, attempt.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        exception,
                        "AI embedding provider {Provider} key {KeyIndex} failed; trying key {NextKeyIndex}.",
                        attempt.ProviderName,
                        attempt.KeyIndex,
                        next.KeyIndex);
                }
                else
                {
                    _logger.LogWarning(
                        exception,
                        "AI embedding provider {Provider} failed; falling back to {NextProvider}.",
                        attempt.ProviderName,
                        next.ProviderName);

                    Activity.Current?.AddEvent(new ActivityEvent(
                        "provider.fallback",
                        tags: new ActivityTagsCollection
                        {
                            { "provider", attempt.ProviderName },
                            { "next.provider", next.ProviderName },
                        }));
                }
            }
        }

        throw new InvalidOperationException("Every AI embedding attempt failed.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        _attempts[0].Generator.GetService(serviceType, serviceKey);

    public void Dispose()
    {
        foreach (var attempt in _attempts)
        {
            attempt.Generator.Dispose();
        }
    }
}
