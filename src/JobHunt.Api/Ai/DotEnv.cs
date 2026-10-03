using Microsoft.Extensions.Configuration;

namespace JobHunt.Api.Ai;

public static class DotEnv
{
    public static void LoadInto(IConfigurationBuilder configuration)
    {
        var path = FindEnvFile();
        if (path is null)
        {
            return;
        }

        var entries = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
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

            var value = Unquote(line[(separator + 1)..].Trim());
            entries[key] = value;

            if (key.Equals("OPENCODE_ZEN_KEY", StringComparison.OrdinalIgnoreCase))
            {
                entries["AI:Zen:ApiKey"] = value;
            }
        }

        if (entries.Count > 0)
        {
            configuration.AddInMemoryCollection(entries);
        }
    }

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