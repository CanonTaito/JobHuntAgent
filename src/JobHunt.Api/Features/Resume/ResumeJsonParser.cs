using System.Text.Json;
using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Features.Resume;

public static class ResumeJsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static CandidateProfile Parse(string raw)
    {
        var json = ExtractJson(raw);

        CandidateProfile? result;
        try
        {
            result = JsonSerializer.Deserialize<CandidateProfile>(json, Options);
        }
        catch (JsonException e)
        {
            throw new ResumeParseFormatException("Model output was not valid JSON.", e);
        }

        if (result is null || IsEmpty(result))
        {
            throw new ResumeParseFormatException("Model output did not contain a usable profile.");
        }

        return result;
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new ResumeParseFormatException("No JSON object found in model output.");
        }

        return raw[start..(end + 1)];
    }

    private static bool IsEmpty(CandidateProfile profile) =>
        string.IsNullOrWhiteSpace(profile.Name)
        && string.IsNullOrWhiteSpace(profile.Headline)
        && string.IsNullOrWhiteSpace(profile.Summary)
        && profile.Skills.Count == 0
        && profile.Experience.Count == 0
        && profile.Education.Count == 0
        && profile.Highlights.Count == 0;
}
