namespace JobHunt.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "ollama";

    public string? EmbeddingProvider { get; set; }

    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProviderOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public List<string> ApiKeys { get; set; } = [];

    public string ChatModel { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public IReadOnlyList<string> EffectiveApiKeys =>
        ApiKeys.Count > 0
            ? ApiKeys
            : string.IsNullOrWhiteSpace(ApiKey)
                ? []
                : [ApiKey];
}
