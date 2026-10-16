using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;

namespace JobHunt.Api.Tests;

public sealed class AiClientFactoryTests
{
    private static ProviderOptions KeylessProvider() => new()
    {
        Endpoint = "http://localhost:11434/v1",
        ChatModel = "gemma3",
        EmbeddingModel = "nomic-embed-text",
        RequiresApiKey = false,
    };

    [Fact]
    public void Builds_a_chat_client_for_a_keyless_provider()
    {
        var provider = KeylessProvider();
        var key = AiProviderKeys.Resolve("ollama", provider)[0];

        using var client = AiClientFactory.CreateChatClient(provider, key);

        Assert.NotNull(client);
    }

    [Fact]
    public void Builds_an_embedding_generator_for_a_keyless_provider()
    {
        var provider = KeylessProvider();
        var key = AiProviderKeys.Resolve("ollama", provider)[0];

        using var generator = AiClientFactory.CreateEmbeddingGenerator(provider, key);

        Assert.NotNull(generator);
    }
}
