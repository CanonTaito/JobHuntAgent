namespace JobHunt.Api.Ai.Providers;

public static class AiProviderResolver
{
    public static ProviderOptions Resolve(AiOptions options) =>
        Resolve(options, options.Provider, nameof(AiOptions.Provider));

    public static ProviderOptions ResolveEmbedding(AiOptions options) =>
        Resolve(
            options,
            string.IsNullOrWhiteSpace(options.EmbeddingProvider) ? options.Provider : options.EmbeddingProvider,
            nameof(AiOptions.EmbeddingProvider));

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
