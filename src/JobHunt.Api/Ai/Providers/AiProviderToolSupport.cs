namespace JobHunt.Api.Ai.Providers;

public static class AiProviderToolSupport
{
    public static bool SupportsTools(AiOptions options, string providerName, ProviderOptions providerOptions)
    {
        if (providerOptions.ChatModel.Contains("llama-3.3", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(providerName, "groq", StringComparison.OrdinalIgnoreCase) ||
            providerOptions.ChatModel.Contains("groq", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!providerOptions.SupportsTools)
        {
            return false;
        }

        return true;
    }
}
