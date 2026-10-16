using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Retry;

public sealed class RetryingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _innerGenerator;
    private readonly AiRetryPolicy _policy;

    public RetryingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, AiRetryPolicy policy)
    {
        _innerGenerator = innerGenerator;
        _policy = policy;
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialized = values as IReadOnlyList<string> ?? values.ToList();

        return _policy.ExecuteAsync(
            "embeddings",
            token => _innerGenerator.GenerateAsync(materialized, options, token),
            cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        _innerGenerator.GetService(serviceType, serviceKey);

    public void Dispose() => _innerGenerator.Dispose();
}
