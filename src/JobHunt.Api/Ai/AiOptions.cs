namespace JobHunt.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "ollama";

    public ZenOptions Zen { get; set; } = new();

    public OllamaOptions Ollama { get; set; } = new();

    public sealed class ZenOptions
    {
        public string Endpoint { get; set; } = "https://opencode.ai/zen/v1";
        public string ChatModel { get; set; } = "big-pickle";
        public string? ApiKey { get; set; }
    }

    public sealed class OllamaOptions
    {
        public string Endpoint { get; set; } = "http://localhost:11434/v1";
        public string ChatModel { get; set; } = "gemma3";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
    }
}