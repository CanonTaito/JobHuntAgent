using JobHunt.Api.Ai;

namespace JobHunt.Api.Tests;

public sealed class DotEnvTests
{
    private static Dictionary<string, string?> Expand(params (string Key, string Value)[] entries)
    {
        var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in entries)
        {
            raw[key] = value;
        }

        return DotEnv.Map(raw);
    }

    [Fact]
    public void Maps_a_single_key_onto_a_provider_key_list()
    {
        var entries = Expand(("GEMINI_API_KEY", "abc"));

        Assert.Equal("abc", entries["AI:Providers:gemini:ApiKeys:0"]);
    }

    [Fact]
    public void Maps_a_comma_separated_key_list_in_order()
    {
        var entries = Expand(("GEMINI_API_KEYS", "a, b, c"));

        Assert.Equal("a", entries["AI:Providers:gemini:ApiKeys:0"]);
        Assert.Equal("b", entries["AI:Providers:gemini:ApiKeys:1"]);
        Assert.Equal("c", entries["AI:Providers:gemini:ApiKeys:2"]);
    }

    [Fact]
    public void Maps_a_json_array_key_list()
    {
        var entries = Expand(("GEMINI_API_KEYS", "[\"a\",\"b\"]"));

        Assert.Equal("a", entries["AI:Providers:gemini:ApiKeys:0"]);
        Assert.Equal("b", entries["AI:Providers:gemini:ApiKeys:1"]);
    }

    [Fact]
    public void Keeps_the_legacy_zen_key_working()
    {
        var entries = Expand(("OPENCODE_ZEN_KEY", "sk-zen"));

        Assert.Equal("sk-zen", entries["AI:Providers:zen:ApiKeys:0"]);
    }

    [Fact]
    public void Prefers_the_list_when_both_forms_are_present()
    {
        var entries = Expand(("GEMINI_API_KEY", "single"), ("GEMINI_API_KEYS", "list"));

        Assert.Equal("list", entries["AI:Providers:gemini:ApiKeys:0"]);
        Assert.DoesNotContain(entries, pair => pair.Value == "single");
    }

    [Fact]
    public void Passes_through_entries_that_are_not_provider_keys()
    {
        var entries = Expand(("AI:Provider", "gemini"), ("FOO", "bar"));

        Assert.Equal("gemini", entries["AI:Provider"]);
        Assert.Equal("bar", entries["FOO"]);
    }

    [Fact]
    public void Passes_through_provider_configuration_written_with_colons()
    {
        var entries = Expand(("AI:Providers:gemini:Endpoint", "https://example/v1"));

        Assert.Equal("https://example/v1", entries["AI:Providers:gemini:Endpoint"]);
    }
}
