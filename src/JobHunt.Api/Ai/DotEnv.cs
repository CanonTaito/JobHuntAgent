using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace JobHunt.Api.Ai;

public static class DotEnv
{
    private const string ApiKeysSuffix = "_API_KEYS";
    private const string ApiKeySuffix = "_API_KEY";
    private const string LegacyZenKey = "OPENCODE_ZEN_KEY";
    private const string LegacyZenProvider = "zen";
    private const string ProviderKeysPrefix = "AI:Providers:";
    private const string ApiKeysSegment = ":ApiKeys";

    public static void LoadInto(IConfigurationBuilder configuration)
    {
        var path = FindEnvFile();
        if (path is null)
        {
            return;
        }

        var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            raw[key] = Unquote(line[(separator + 1)..].Trim());
        }

        if (raw.Count > 0)
        {
            configuration.AddInMemoryCollection(Map(raw));
        }
    }

    public static Dictionary<string, string?> Map(IReadOnlyDictionary<string, string?> raw)
    {
        var entries = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var singles = new List<KeyValuePair<string, string>>();

        var multiProviders = raw.Keys
            .Where(key => key.EndsWith(ApiKeysSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(key => key[..^ApiKeysSuffix.Length])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in raw)
        {
            if (value is null)
            {
                continue;
            }

            if (key.EndsWith(ApiKeysSuffix, StringComparison.OrdinalIgnoreCase))
            {
                Collect(lists, key[..^ApiKeysSuffix.Length], ParseList(value));
            }
            else if (key.EndsWith(ApiKeySuffix, StringComparison.OrdinalIgnoreCase))
            {
                var provider = key[..^ApiKeySuffix.Length];
                if (!multiProviders.Contains(provider))
                {
                    singles.Add(new KeyValuePair<string, string>(provider, value));
                }
            }
            else if (key.Equals(LegacyZenKey, StringComparison.OrdinalIgnoreCase))
            {
                Collect(lists, LegacyZenProvider, [value]);
            }
            else
            {
                entries[key] = value;
            }
        }

        foreach (var single in singles)
        {
            Collect(lists, single.Key, [single.Value]);
        }

        foreach (var (provider, keys) in lists)
        {
            var name = provider.ToLowerInvariant();
            for (var index = 0; index < keys.Count; index++)
            {
                entries[$"{ProviderKeysPrefix}{name}{ApiKeysSegment}:{index}"] = keys[index];
            }
        }

        return entries;
    }

    private static void Collect(Dictionary<string, List<string>> lists, string provider, List<string> keys)
    {
        if (!lists.TryGetValue(provider, out var existing))
        {
            existing = [];
            lists[provider] = existing;
        }

        existing.AddRange(keys);
    }

    private static List<string> ParseList(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length >= 2 && trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            List<string>? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
            }
            catch (JsonException)
            {
                parsed = null;
            }

            if (parsed is not null)
            {
                return Clean(parsed);
            }
        }

        return Clean(trimmed.Split(','));
    }

    private static List<string> Clean(IEnumerable<string> items) =>
        items.Select(item => item.Trim()).Where(item => item.Length > 0).ToList();

    private static string? FindEnvFile()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string Unquote(string value) =>
        value.Length >= 2
        && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
            ? value[1..^1]
            : value;
}
