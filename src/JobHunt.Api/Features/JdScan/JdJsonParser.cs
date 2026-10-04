using System.Text.Json;

namespace JobHunt.Api.Features.JdScan;

public static class JdJsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static JdAnalysis Parse(string raw)
    {
        var json = ExtractJson(raw);

        JdAnalysis? result;
        try
        {
            result = JsonSerializer.Deserialize<JdAnalysis>(json, Options);
        }
        catch (JsonException e)
        {
            throw new JdScanFormatException("Model output was not valid JSON.", e);
        }

        if (result is null || string.IsNullOrWhiteSpace(result.Title))
        {
            throw new JdScanFormatException("Model output was missing the required title field.");
        }

        return result;
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new JdScanFormatException("No JSON object found in model output.");
        }

        return raw[start..(end + 1)];
    }
}