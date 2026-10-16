using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;

namespace JobHunt.Api.Tests;

public sealed class AiChatPlannerTests
{
    private static readonly ChatResponse Success = new([new ChatMessage(ChatRole.Assistant, "ok")]);

    private static AiOptions Options(string provider, params string[] fallbacks) => new()
    {
        Provider = provider,
        FallbackProviders = [.. fallbacks],
        Providers =
        {
            ["ollama"] = new ProviderOptions { Endpoint = "http://localhost:11434/v1", ChatModel = "gemma3", ApiKey = "ollama-key" },
            ["gemini"] = new ProviderOptions { Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/", ChatModel = "gemini-2.5-flash", ApiKeys = ["g1", "g2"] },
            ["groq"] = new ProviderOptions { Endpoint = "https://api.groq.com/openai/v1", ChatModel = "llama-3.3-70b-versatile", ApiKey = "q1" },
            ["zen"] = new ProviderOptions { Endpoint = "https://opencode.ai/zen/v1", ChatModel = "big-pickle" },
            ["local"] = new ProviderOptions { Endpoint = "http://localhost:8080/v1", ChatModel = "local-model", RequiresApiKey = false },
        },
    };

    private static (IReadOnlyList<AiChatAttempt> Attempts, List<(string Model, string Key)> Calls, List<(string Name, string Reason)> Skipped) Build(
        AiOptions options)
    {
        var calls = new List<(string Model, string Key)>();
        var skipped = new List<(string Name, string Reason)>();

        var attempts = AiChatPlanner.BuildAttempts(
            options,
            (provider, key) =>
            {
                calls.Add((provider.ChatModel, key));
                return new FakeChatClient(Success);
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
        Assert.Equal([("gemini-2.5-flash", "g1"), ("gemini-2.5-flash", "g2")], calls);
        Assert.Empty(skipped);
    }

    [Fact]
    public void Falls_back_through_the_configured_provider_order()
    {
        var (attempts, calls, skipped) = Build(Options("ollama", "gemini", "groq"));

        Assert.Equal(["ollama", "gemini", "gemini", "groq"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Equal([0, 0, 1, 0], attempts.Select(attempt => attempt.KeyIndex).ToArray());
        Assert.Equal(["ollama-key", "g1", "g2", "q1"], calls.Select(call => call.Key).ToArray());
        Assert.Empty(skipped);
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
        Assert.Equal("local-model", calls[1].Model);
        Assert.Equal(AiProviderKeys.KeylessApiKey, calls[1].Key);
        Assert.Empty(skipped);
    }

    [Fact]
    public void Throws_when_the_primary_provider_has_no_key()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Build(Options("zen")));

        Assert.Contains("ZEN_API_KEY", exception.Message);
    }

    [Fact]
    public void Throws_for_an_unknown_fallback_provider()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Build(Options("ollama", "gemini2")));

        Assert.Contains("gemini2", exception.Message);
        Assert.Contains(nameof(AiOptions.FallbackProviders), exception.Message);
    }

    [Fact]
    public void Ignores_blank_fallback_entries()
    {
        var (attempts, _, skipped) = Build(Options("ollama", "", "   ", "groq"));

        Assert.Equal(["ollama", "groq"], attempts.Select(attempt => attempt.ProviderName).ToArray());
        Assert.Empty(skipped);
    }

    [Fact]
    public void Treats_a_fallback_that_repeats_the_primary_as_the_primary()
    {
        var (attempts, _, _) = Build(Options("ollama", "OLLAMA", "groq"));

        Assert.Equal(["ollama", "groq"], attempts.Select(attempt => attempt.ProviderName).ToArray());
    }
}
