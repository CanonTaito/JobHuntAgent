using JobHunt.Api.Ai;

namespace JobHunt.Api.Tests;

public sealed class ProviderOptionsTests
{
    [Fact]
    public void Effective_api_keys_prefer_the_configured_list()
    {
        var provider = new ProviderOptions
        {
            ApiKey = "singular",
            ApiKeys = ["first", "second"],
        };

        Assert.Equal(["first", "second"], provider.EffectiveApiKeys);
    }

    [Fact]
    public void Effective_api_keys_fall_back_to_the_singular_key()
    {
        var provider = new ProviderOptions { ApiKey = "singular" };

        Assert.Equal(["singular"], provider.EffectiveApiKeys);
    }

    [Fact]
    public void Effective_api_keys_are_empty_when_nothing_is_configured()
    {
        var provider = new ProviderOptions();

        Assert.Empty(provider.EffectiveApiKeys);
    }
}
