using System.Text.Json;

namespace JobHunt.Api.Features.Match;

public static class MatchScoreJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static MatchScore Parse(string raw)
    {
        var json = ExtractJson(raw);

        RawMatchScore? result;
        try
        {
            result = JsonSerializer.Deserialize<RawMatchScore>(json, Options);
        }
        catch (JsonException e)
        {
            throw new MatchScoreFormatException("Model output was not valid JSON.", e);
        }

        if (result is null)
        {
            throw new MatchScoreFormatException("Model output was not a JSON object.");
        }

        var skills = Normalize(result.Skills);
        var seniority = Normalize(result.Seniority);
        var fit = Normalize(result.Fit);
        if (skills is null || seniority is null || fit is null)
        {
            throw new MatchScoreFormatException("Model output was missing the required skills, seniority, or fit fields.");
        }

        return new MatchScore
        {
            Skills = skills.Value,
            Seniority = seniority.Value,
            Fit = fit.Value,
            Summary = result.Summary?.Trim() ?? string.Empty,
            Strengths = Clean(result.Strengths),
            Gaps = Clean(result.Gaps),
        };
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new MatchScoreFormatException("No JSON object found in model output.");
        }

        return raw[start..(end + 1)];
    }

    private static int? Normalize(double? value) =>
        value is null ? null : Math.Clamp((int)Math.Round(value.Value), 0, 100);

    private static IReadOnlyList<string> Clean(IReadOnlyList<string>? items) =>
        items is null
            ? []
            : items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();

    private sealed class RawMatchScore
    {
        public double? Skills { get; init; }

        public double? Seniority { get; init; }

        public double? Fit { get; init; }

        public string? Summary { get; init; }

        public IReadOnlyList<string>? Strengths { get; init; }

        public IReadOnlyList<string>? Gaps { get; init; }
    }
}
