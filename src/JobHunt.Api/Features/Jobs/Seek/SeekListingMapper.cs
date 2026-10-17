using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobHunt.Api.Features.Jobs.Seek;

public static class SeekListingMapper
{
    private static readonly Regex SalaryPattern =
        new(@"(?<num>\d{1,3}(?:,\d{3})+|\d+(?:\.\d+)?)\s*(?<unit>[kK])?", RegexOptions.Compiled);

    public static JobListing? Map(SeekAd ad)
    {
        var id = ResolveId(ad.Id);
        var title = ad.Title?.Trim();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var (min, max) = ParseSalary(ad.SalaryLabel);

        return new JobListing
        {
            Id = id,
            Source = SeekJobSource.SourceName,
            Title = title,
            Company = FirstNonEmpty(ad.CompanyName, ad.Advertiser?.Description) ?? "Unknown",
            Location = Blank(ad.Locations?.FirstOrDefault()?.Label),
            WorkModel = MapWorkModel(ResolveWorkArrangement(ad)),
            Description = BuildDescription(ad),
            SalaryMin = min,
            SalaryMax = max,
            SalaryCurrency = min is not null || max is not null ? "AUD" : null,
            SalaryText = Blank(ad.SalaryLabel),
            Url = $"https://www.seek.com.au/job/{id}",
            PostedAt = ParseDate(ad.ListingDate),
        };
    }

    private static string? ResolveWorkArrangement(SeekAd ad) =>
        FirstNonEmpty(ad.WorkArrangements?.DisplayText, ad.WorkArrangements?.Data?.FirstOrDefault()?.Label?.Text);

    private static string BuildDescription(SeekAd ad)
    {
        var parts = new List<string>();
        var teaser = Blank(ad.Teaser);
        if (teaser is not null)
        {
            parts.Add(teaser);
        }

        if (ad.BulletPoints is { Count: > 0 } bullets)
        {
            foreach (var bullet in bullets)
            {
                var text = Blank(bullet);
                if (text is not null)
                {
                    parts.Add($"- {text}");
                }
            }
        }

        return string.Join("\n", parts);
    }

    public static (int? Min, int? Max) ParseSalary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        var values = new List<int>();
        foreach (System.Text.RegularExpressions.Match match in SalaryPattern.Matches(text))
        {
            if (!decimal.TryParse(
                    match.Groups["num"].Value.Replace(",", string.Empty),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                continue;
            }

            var thousands = match.Groups["unit"].Success || number < 1000;
            var value = (int)Math.Round(thousands ? number * 1000 : number);
            if (value is < 10_000 or > 2_000_000)
            {
                continue;
            }

            values.Add(value);
        }

        if (values.Count == 0)
        {
            return (null, null);
        }

        if (values.Count == 1)
        {
            return (values[0], null);
        }

        values.Sort();
        return (values[0], values[^1]);
    }

    public static string? MapWorkModel(string? arrangement)
    {
        var value = arrangement?.Trim().ToLowerInvariant();
        return value switch
        {
            "remote" => "Remote",
            "hybrid" => "Hybrid",
            "onsite" or "on-site" or "on site" => "On-site",
            _ => null,
        };
    }

    private static string ResolveId(JsonElement? id) => id?.ValueKind switch
    {
        JsonValueKind.Number => id.Value.GetInt64().ToString(CultureInfo.InvariantCulture),
        JsonValueKind.String => id.Value.GetString() ?? string.Empty,
        _ => string.Empty,
    };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed.UtcDateTime)
            : null;
}
