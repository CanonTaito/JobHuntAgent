namespace JobHunt.Api.Ai.Providers;

public static class AiProviderKeys
{
    public static IReadOnlyList<string> Resolve(string providerName, ProviderOptions provider)
    {
        var keys = provider.EffectiveApiKeys;
        if (keys.Count > 0)
        {
            return keys;
        }

        if (provider.RequiresApiKey)
        {
            throw new InvalidOperationException(
                $"AI provider '{providerName}' requires an API key but none is configured. " +
                $"Add '{providerName.ToUpperInvariant()}_API_KEY' to .env or set " +
                $"'{AiOptions.SectionName}:Providers:{providerName}:{nameof(ProviderOptions.RequiresApiKey)}' to false.");
        }

        return [string.Empty];
    }
}
