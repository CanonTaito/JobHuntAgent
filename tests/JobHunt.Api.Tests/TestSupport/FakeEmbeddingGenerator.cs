using Microsoft.Extensions.AI;

namespace JobHunt.Api.Tests.TestSupport;

public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly Func<string, float[]> _vector;
    private readonly Queue<Exception> _failures;

    public FakeEmbeddingGenerator(Func<string, float[]> vector, params Exception[] failures)
    {
        _vector = vector;
        _failures = new Queue<Exception>(failures);
    }

    public int Calls { get; private set; }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        if (_failures.Count > 0)
        {
            return Task.FromException<GeneratedEmbeddings<Embedding<float>>>(_failures.Dequeue());
        }

        var embeddings = values.Select(value => new Embedding<float>(_vector(value))).ToList();
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
