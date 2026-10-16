using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;

namespace JobHunt.Api.Tests;

public sealed class AiEmbeddingPlannerTests
{
    private static AiOptions Options(string provider, params string[] fallbacks) => new()
    {
        Provider = provider,
        FallbackProviders = [.. fallbacks],
        Providers =
        {
            ["ollama"] = new ProviderOptions
            {
                Endpoint = "http://localhost:11434/v1",
                ChatModel = "gemma3",
                EmbeddingModel = "nomic-embed-text",
                ApiKey = "ollama-key",
            },
            ["gemini"] = new ProviderOptions
            {
                Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
                ChatModel = "gemini-2.5-flash",
                EmbeddingModel = "gemini-embedding-001",
                ApiKeys = ["g1", "g2"],
            },
            ["groq"] = new ProviderOptions
            {
                Endpoint = "https://api.groq.com/openai/v1",
                ChatModel = "llama-3.3-70b-versatile",
                ApiKey = "q1",
            },
            ["zen"] = new ProviderOptions
            {
                Endpoint = "https://opencode.ai/zen/v1",
                ChatModel = "big-pickle",
                EmbeddingModel = "text-embedding-3-small",
            },
            ["local"] = new ProviderOptions
            {
                Endpoint = "http://localhost:8080/v1",
                ChatModel = "local-model",
                EmbeddingModel = "local-embed",
                RequiresApiKey = false,
            },
        },
    };

    private static (IReadOnlyList<AiEmbeddingAttempt> Attempts, List<(string Model, string Key)> Calls, List<(string Name, string Reason)> Skipped) Build(
        AiOptions options)
    {
        var calls = new List<(string Model, string Key)>();
        var skipped = new List<(string Name, string Reason)>();

        var attempts = AiEmbeddingPlanner.BuildAttempts(
            options,
            (provider, key) =>
            {
                calls.Add((provider.EmbeddingModel, key));
                return new FakeEmbeddingGenerator(_ => [1f, 0f]);
            },
            (name, reason) => skipped.Add((name, reason)));

        return (attempts, calls, skipped);
    }

    [Fact]
    public void Plans_one_attempt_per_key_of_the_primary_provider()
    {
        var (attempts, calls, skipped) = Build(Options("gemini"));

        Assert.Equal(["gemini", "gemini"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal([0, 1], attempts.Select(attempt => attempt.KeyIndex).ToArray());
        Assert.Equal([("gemini-embedding-001", "g1"), ("gemini-embedding-001", "g2")], calls);
        Assert.Empty(skipped);
    }

    [Fact]
    public void Uses_the_explicit_embedding_provider_as_the_chain_head()
    {
        var options = Options("ollama", "local");
        options.EmbeddingProvider = "gemini";

        var (attempts, _, skipped) = Build(options);

        Assert.Equal(["gemini", "gemini", "local"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Empty(skipped);
    }

    [Fact]
    public void Falls_back_through_the_configured_provider_order()
    {
        var (attempts, calls, skipped) = Build(Options("ollama", "gemini"));

        Assert.Equal(["ollama", "gemini", "gemini"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal([0, 0, 1], attempts.Select(attempt => attempt.KeyIndex).ToArray());
        Assert.Equal(["ollama-key", "g1", "g2"], calls.Select(call => call.Key).ToArray());
        Assert.Empty(skipped);
    }

    [Fact]
    public void Skips_a_fallback_without_an_embedding_model()
    {
        var (attempts, _, skipped) = Build(Options("ollama", "groq"));

        Assert.Equal(["ollama"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal(["groq"], skipped.Select(skip => skip.Name).ToArray());
        Assert.Contains("no embedding model", skipped[0].Reason);
    }

    [Fact]
    public void Skips_a_fallback_that_has_no_key_and_reports_the_reason()
    {
        var (attempts, _, skipped) = Build(Options("ollama", "zen"));

        Assert.Equal(["ollama"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal(["zen"], skipped.Select(skip => skip.Name).ToArray());
        Assert.Contains("ZEN_API_KEY", skipped[0].Reason);
    }

    [Fact]
    public void Keeps_a_keyless_fallback()
    {
        var (attempts, calls, skipped) = Build(Options("ollama", "local"));

        Assert.Equal(["ollama", "local"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal("local-embed", calls[1].Model);
        Assert.Equal(AiProviderKeys.KeylessApiKey, calls[1].Key);
        Assert.Empty(skipped);
    }

    [Fact]
    public void Throws_when_the_primary_provider_has_no_embedding_model()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Build(Options("groq")));

        Assert.Contains("cannot be used for embeddings", exception.Message);
    }

    [Fact]
    public void Throws_when_the_primary_provider_has_no_key()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Build(Options("zen")));

        Assert.Contains("ZEN_API_KEY", exception.Message);
    }

    [Fact]
    public void Ignores_blank_fallback_entries()
    {
        var (attempts, _, skipped) = Build(Options("ollama", "", "   ", "gemini"));

        Assert.Equal(["ollama", "gemini", "gemini"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Empty(skipped);
    }

    [Fact]
    public void Treats_a_fallback_that_repeats_the_primary_as_the_primary()
    {
        var (attempts, _, _) = Build(Options("ollama", "OLLAMA", "gemini"));

        Assert.Equal(["ollama", "gemini", "gemini"], attempts.Select(attempt => attempt.ProviderName).ToArray());
    }
}
