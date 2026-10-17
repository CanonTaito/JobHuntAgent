using System.Text.Json;

namespace JobHunt.Api.Features.Tailoring;

public static class TailoringJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static TailoringDraft Parse(string raw)
    {
        var json = ExtractJson(raw);

        RawTailoringDraft? result;
        try
        {
            result = JsonSerializer.Deserialize<RawTailoringDraft>(json, Options);
        }
        catch (JsonException e)
        {
            throw new TailoringFormatException("Model output was not valid JSON.", e);
        }

        if (result is null)
        {
            throw new TailoringFormatException("Model output was not a JSON object.");
        }

        var bullets = Clean(result.ResumeBullets);
        var coverLetter = result.CoverLetter?.Trim();

        if ((bullets.Count == 0 || string.IsNullOrWhiteSpace(coverLetter)) && result.TailoredProfile is null)
        {
            throw new TailoringFormatException("Model output was missing resume bullets/cover letter and did not include a tailored profile.");
        }

        return new TailoringDraft
        {
            ResumeBullets = bullets,
            CoverLetter = coverLetter ?? string.Empty,
            TailoredProfile = result.TailoredProfile,
        };
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new TailoringFormatException("No JSON object found in model output.");
        }

        return raw[start..(end + 1)];
    }

    private static IReadOnlyList<string> Clean(IReadOnlyList<string>? items) =>
        items is null
            ? []
            : items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();

    private sealed class RawTailoringDraft
    {
        public IReadOnlyList<string>? ResumeBullets { get; init; }

        public string? CoverLetter { get; init; }

        public JobHunt.Api.Features.Profile.CandidateProfile? TailoredProfile { get; init; }
    }
}
