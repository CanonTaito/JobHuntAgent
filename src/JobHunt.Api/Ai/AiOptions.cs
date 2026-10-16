using JobHunt.Api.Ai.Retry;

namespace JobHunt.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "ollama";

    public List<string> FallbackProviders { get; set; } = [];

    public string? EmbeddingProvider { get; set; }

    public AiRetryOptions Retry { get; set; } = new();

    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProviderOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public List<string> ApiKeys { get; set; } = [];

    public bool RequiresApiKey { get; set; } = true;

    public string ChatModel { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public bool SupportsTools { get; set; } = true;

    public IReadOnlyList<string> EffectiveApiKeys =>
        ApiKeys.Count > 0
            ? ApiKeys
            : string.IsNullOrWhiteSpace(ApiKey)
                ? []
                : [ApiKey];
}
