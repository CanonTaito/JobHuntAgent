namespace JobHunt.Api.Ai.Providers;

public static class AiProviderKeys
{
    public const string KeylessApiKey = "keyless";

    public static IReadOnlyList<string> Resolve(string providerName, ProviderOptions provider) =>
        TryResolve(providerName, provider, out var keys)
            ? keys
            : throw new InvalidOperationException(MissingKeyMessage(providerName));

    public static bool TryResolve(string providerName, ProviderOptions provider, out IReadOnlyList<string> keys)
    {
        var effective = provider.EffectiveApiKeys;
        if (effective.Count > 0)
        {
            keys = effective;
            return true;
        }

        if (provider.RequiresApiKey)
        {
            keys = [];
            return false;
        }

        keys = [KeylessApiKey];
        return true;
    }

    public static string MissingKeyMessage(string providerName) =>
        $"AI provider '{providerName}' requires an API key but none is configured. " +
        $"Add '{providerName.ToUpperInvariant()}_API_KEY' to .env or set " +
        $"'{AiOptions.SectionName}:Providers:{providerName}:{nameof(ProviderOptions.RequiresApiKey)}' to false.";
}
