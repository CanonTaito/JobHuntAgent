using System.Text.Json;
using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;

namespace JobHunt.Api.Tests;

public sealed class ShippedAiConfigTests
{
    private static AiOptions LoadShippedOptions()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "src", "JobHunt.Api", "appsettings.json");
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));

                return JsonSerializer.Deserialize<AiOptions>(
                    document.RootElement.GetProperty("AI").GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("The shipped AI configuration could not be parsed.");
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find src/JobHunt.Api/appsettings.json above the test output directory.");
    }

    [Fact]
    public void Defaults_to_the_local_ollama_provider()
    {
        var options = LoadShippedOptions();

        Assert.Equal("ollama", options.Provider);

        var resolved = AiProviderResolver.Resolve(options);
        Assert.Equal("http://localhost:11434/v1", resolved.Endpoint);

        Assert.Equal(resolved.Endpoint, AiProviderResolver.ResolveEmbedding(options).Endpoint);
    }

    [Fact]
    public void Ships_a_gemini_provider_with_chat_and_embedding_models()
    {
        var options = LoadShippedOptions();
        options.Provider = "gemini";

        var provider = AiProviderResolver.Resolve(options);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/openai/", provider.Endpoint);
        Assert.Equal("gemini-2.5-flash", provider.ChatModel);

        Assert.Equal("gemini-embedding-001", AiProviderResolver.ResolveEmbedding(options).EmbeddingModel);
    }

    [Fact]
    public void Ships_a_groq_provider_without_embeddings()
    {
        var options = LoadShippedOptions();
        options.Provider = "groq";

        var provider = AiProviderResolver.Resolve(options);
        Assert.Equal("https://api.groq.com/openai/v1", provider.Endpoint);
        Assert.Equal("llama-3.3-70b-versatile", provider.ChatModel);
        Assert.True(string.IsNullOrWhiteSpace(provider.EmbeddingModel));

        Assert.Throws<InvalidOperationException>(() => AiProviderResolver.ResolveEmbedding(options));
    }
}
