using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;

namespace JobHunt.Api.Tests;

public sealed class AiProviderKeysTests
{
    [Fact]
    public void Uses_every_configured_key_in_order()
    {
        var provider = new ProviderOptions { ApiKeys = ["first", "second"] };

        Assert.Equal(["first", "second"], AiProviderKeys.Resolve("gemini", provider));
    }

    [Fact]
    public void Falls_back_to_the_singular_key()
    {
        var provider = new ProviderOptions { ApiKey = "singular", RequiresApiKey = true };

        Assert.Equal(["singular"], AiProviderKeys.Resolve("gemini", provider));
    }

    [Fact]
    public void Throws_when_a_required_key_is_missing()
    {
        var provider = new ProviderOptions();

        var exception = Assert.Throws<InvalidOperationException>(() => AiProviderKeys.Resolve("gemini", provider));
        Assert.Contains("GEMINI_API_KEY", exception.Message);
        Assert.Contains("RequiresApiKey", exception.Message);
    }

    [Fact]
    public void Allows_a_keyless_provider_when_a_key_is_not_required()
    {
        var provider = new ProviderOptions { RequiresApiKey = false };

        Assert.Equal([AiProviderKeys.KeylessApiKey], AiProviderKeys.Resolve("ollama", provider));
    }

    [Fact]
    public void TryResolve_returns_the_configured_keys()
    {
        var provider = new ProviderOptions { ApiKeys = ["first", "second"] };

        Assert.True(AiProviderKeys.TryResolve("gemini", provider, out var keys));
        Assert.Equal(["first", "second"], keys);
    }

    [Fact]
    public void TryResolve_reports_a_missing_required_key()
    {
        var provider = new ProviderOptions();

        Assert.False(AiProviderKeys.TryResolve("gemini", provider, out var keys));
        Assert.Empty(keys);
    }

    [Fact]
    public void TryResolve_supplies_a_placeholder_key_when_a_key_is_not_required()
    {
        var provider = new ProviderOptions { RequiresApiKey = false };

        Assert.True(AiProviderKeys.TryResolve("ollama", provider, out var keys));
        Assert.Equal([AiProviderKeys.KeylessApiKey], keys);
    }

    [Fact]
    public void Missing_key_message_names_the_env_var_and_the_setting()
    {
        var message = AiProviderKeys.MissingKeyMessage("gemini");

        Assert.Contains("GEMINI_API_KEY", message);
        Assert.Contains("AI:Providers:gemini:RequiresApiKey", message);
    }
}
