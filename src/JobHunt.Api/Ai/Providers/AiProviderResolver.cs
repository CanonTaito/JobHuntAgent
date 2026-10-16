namespace JobHunt.Api.Ai.Providers;

public static class AiProviderResolver
{
    public static ProviderOptions Resolve(AiOptions options)
    {
        if (options.Providers.Count == 0)
        {
            throw new InvalidOperationException(
                $"No AI providers are configured. Add one under '{AiOptions.SectionName}:Providers'.");
        }

        var match = options.Providers.FirstOrDefault(pair =>
            string.Equals(pair.Key, options.Provider, StringComparison.OrdinalIgnoreCase));

        if (match.Value is null)
        {
            throw new InvalidOperationException(
                $"Unknown AI provider '{options.Provider}'. Configured providers: " +
                $"{string.Join(", ", options.Providers.Keys)}.");
        }

        return match.Value;
    }
}
