using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Providers;

public static class AiChatPlanner
{
    public static IReadOnlyList<AiChatAttempt> BuildAttempts(
        AiOptions options,
        Func<ProviderOptions, string, IChatClient> createClient,
        Action<string, string> onProviderSkipped)
    {
        var chain = new List<string> { options.Provider };
        chain.AddRange(options.FallbackProviders.Where(name => !string.IsNullOrWhiteSpace(name)));

        var attempts = new List<AiChatAttempt>();
        var planned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var position = 0; position < chain.Count; position++)
        {
            var providerName = chain[position];

            if (!planned.Add(providerName))
            {
                continue;
            }

            var provider = position == 0
                ? AiProviderResolver.Resolve(options)
                : AiProviderResolver.ResolveFallback(options, providerName);

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
                attempts.Add(new AiChatAttempt(providerName, keyIndex, createClient(provider, keys[keyIndex])));
            }
        }

        return attempts;
    }
}
