using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;

namespace JobHunt.Api.Tests;

public sealed class AiProviderResolverTests
{
    private static AiOptions Options(string provider) => new()
    {
        Provider = provider,
        Providers =
        {
            ["ollama"] = new ProviderOptions { Endpoint = "http://localhost:11434/v1", ChatModel = "gemma3" },
            ["zen"] = new ProviderOptions { Endpoint = "https://opencode.ai/zen/v1", ChatModel = "big-pickle" },
        },
    };

    [Fact]
    public void Resolves_the_selected_provider()
    {
        var resolved = AiProviderResolver.Resolve(Options("zen"));

        Assert.Equal("https://opencode.ai/zen/v1", resolved.Endpoint);
        Assert.Equal("big-pickle", resolved.ChatModel);
    }

    [Fact]
    public void Resolves_provider_names_case_insensitively()
    {
        var resolved = AiProviderResolver.Resolve(Options("OLLAMA"));

        Assert.Equal("gemma3", resolved.ChatModel);
    }

    [Fact]
    public void Throws_for_an_unknown_provider_and_lists_the_configured_ones()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => AiProviderResolver.Resolve(Options("gemini")));

        Assert.Contains("gemini", exception.Message);
        Assert.Contains("ollama", exception.Message);
        Assert.Contains("zen", exception.Message);
    }

    [Fact]
    public void Throws_when_no_providers_are_configured()
    {
        var options = new AiOptions { Provider = "ollama" };

        Assert.Throws<InvalidOperationException>(() => AiProviderResolver.Resolve(options));
    }
}
