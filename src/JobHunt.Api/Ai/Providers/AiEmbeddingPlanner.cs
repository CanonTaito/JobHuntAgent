using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Providers;

public static class AiEmbeddingPlanner
{
    public static IReadOnlyList<AiEmbeddingAttempt> BuildAttempts(
        AiOptions options,
        Func<ProviderOptions, string, IEmbeddingGenerator<string, Embedding<float>>> createGenerator,
        Action<string, string> onProviderSkipped)
    {
        var chain = new List<string> { AiProviderResolver.EmbeddingProviderName(options) };
        chain.AddRange(options.FallbackProviders.Where(name => !string.IsNullOrWhiteSpace(name)));

        var attempts = new List<AiEmbeddingAttempt>();
        var planned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var position = 0; position < chain.Count; position++)
        {
            var providerName = chain[position];

            if (!planned.Add(providerName))
            {
                continue;
            }

            var provider = position == 0
                ? AiProviderResolver.ResolveEmbedding(options)
                : AiProviderResolver.ResolveFallback(options, providerName);

            if (position > 0 && string.IsNullOrWhiteSpace(provider.EmbeddingModel))
            {
                onProviderSkipped(providerName, $"AI provider '{providerName}' has no embedding model.");
                continue;
            }

            IReadOnlyList<string> keys;

            if (position == 0)
            {
                keys = AiProviderKeys.Resolve(providerName, provider);
            }
            else if (AiProviderKeys.TryResolve(providerName, provider, out var fallbackKeys))
            {
                keys = fallbackKeys;
            }
            else
            {
                onProviderSkipped(providerName, AiProviderKeys.MissingKeyMessage(providerName));
                continue;
            }

            for (var keyIndex = 0; keyIndex < keys.Count; keyIndex++)
            {
                attempts.Add(new AiEmbeddingAttempt(providerName, keyIndex, createGenerator(provider, keys[keyIndex])));
            }
        }

        return attempts;
    }
}
