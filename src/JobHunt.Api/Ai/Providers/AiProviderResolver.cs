namespace JobHunt.Api.Ai.Providers;

public static class AiProviderResolver
{
    public static ProviderOptions Resolve(AiOptions options) =>
        Resolve(options, options.Provider, nameof(AiOptions.Provider));

    public static ProviderOptions ResolveEmbedding(AiOptions options)
    {
        var explicitProvider = !string.IsNullOrWhiteSpace(options.EmbeddingProvider);
        var providerName = explicitProvider ? options.EmbeddingProvider! : options.Provider;

        var provider = Resolve(
            options,
            providerName,
            explicitProvider ? nameof(AiOptions.EmbeddingProvider) : nameof(AiOptions.Provider));

        if (string.IsNullOrWhiteSpace(provider.EmbeddingModel))
        {
            var withEmbeddings = options.Providers
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value.EmbeddingModel))
                .Select(pair => pair.Key);

            throw new InvalidOperationException(
                $"AI provider '{providerName}' does not provide an embedding model, so it cannot be used for " +
                $"embeddings. Set '{AiOptions.SectionName}:{nameof(AiOptions.EmbeddingProvider)}' to a provider " +
                $"that has one: {string.Join(", ", withEmbeddings)}.");
        }

        return provider;
    }

    private static ProviderOptions Resolve(AiOptions options, string providerName, string settingName)
    {
        if (options.Providers.Count == 0)
        {
            throw new InvalidOperationException(
                $"No AI providers are configured. Add one under '{AiOptions.SectionName}:Providers'.");
        }

        var match = options.Providers.FirstOrDefault(pair =>
            string.Equals(pair.Key, providerName, StringComparison.OrdinalIgnoreCase));

        if (match.Value is null)
        {
            throw new InvalidOperationException(
                $"Unknown AI provider '{providerName}' (from '{AiOptions.SectionName}:{settingName}'). " +
                $"Configured providers: {string.Join(", ", options.Providers.Keys)}.");
        }

        return match.Value;
    }
}
