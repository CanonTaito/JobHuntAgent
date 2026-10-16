using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;

namespace JobHunt.Api.Tests;

public sealed class AiProviderToolSupportTests
{
    [Fact]
    public void Groq_provider_is_marked_as_tool_unavailable()
    {
        var options = new AiOptions
        {
            Provider = "groq",
            Providers =
            {
                ["groq"] = new ProviderOptions { Endpoint = "https://api.groq.com/openai/v1", ChatModel = "llama-3.3-70b-versatile" },
            },
        };

        var provider = AiProviderResolver.Resolve(options);
        Assert.False(AiProviderToolSupport.SupportsTools(options, "groq", provider));
    }

    [Fact]
    public void Defaults_to_tool_support()
    {
        var options = new AiOptions
        {
            Provider = "ollama",
            Providers =
            {
                ["ollama"] = new ProviderOptions { Endpoint = "http://localhost:11434/v1", ChatModel = "gemma3" },
            },
        };

        var provider = AiProviderResolver.Resolve(options);
        Assert.True(AiProviderToolSupport.SupportsTools(options, "ollama", provider));
    }

    [Fact]
    public void Can_be_explicitly_disabled()
    {
        var options = new AiOptions
        {
            Provider = "ollama",
            Providers =
            {
                ["ollama"] = new ProviderOptions { Endpoint = "http://localhost:11434/v1", ChatModel = "gemma3", SupportsTools = false },
            },
        };

        var provider = AiProviderResolver.Resolve(options);
        Assert.False(AiProviderToolSupport.SupportsTools(options, "ollama", provider));
    }
}
