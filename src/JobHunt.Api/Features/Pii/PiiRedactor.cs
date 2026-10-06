using System.Text;
using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Pii;

public sealed class PiiRedactor
{
    private readonly PiiMapping[] _redactOrder;
    private readonly Dictionary<string, string> _rehydrate;

    public PiiRedactor(CandidateProfile profile, IPiiMappingStore store, ILogger<PiiRedactor> logger)
    {
        foreach (var (value, placeholder) in ExtractCandidates(profile))
        {
            store.Put(new PiiMapping(value, placeholder));
        }

        var mappings = store.GetAll();
        _redactOrder = mappings.OrderByDescending(m => m.Value.Length).ToArray();

        _rehydrate = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            _rehydrate.TryAdd(mapping.Placeholder, mapping.Value);
        }

        logger.LogInformation("Prepared {Count} PII placeholder mappings", mappings.Count);
    }

    public string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        foreach (var mapping in _redactOrder)
        {
            text = ReplaceToken(text, mapping.Value, mapping.Placeholder);
        }

        return text;
    }

    public CandidateProfile Redact(CandidateProfile profile) => profile with
    {
        Name = Redact(profile.Name),
        Headline = RedactOrNull(profile.Headline),
        Location = RedactOrNull(profile.Location),
        Summary = RedactOrNull(profile.Summary),
        Skills = RedactAll(profile.Skills),
        Experience = profile.Experience
            .Select(entry => entry with
            {
                Company = RedactOrNull(entry.Company),
                Title = RedactOrNull(entry.Title),
                Period = RedactOrNull(entry.Period),
                Highlights = RedactAll(entry.Highlights),
            })
            .ToList(),
        Education = profile.Education
            .Select(entry => entry with
            {
                Qualification = RedactOrNull(entry.Qualification),
                Institution = RedactOrNull(entry.Institution),
            })
            .ToList(),
        Highlights = RedactAll(profile.Highlights),
        Preferences = profile.Preferences with
        {
            Roles = RedactAll(profile.Preferences.Roles),
            Locations = RedactAll(profile.Preferences.Locations),
            WorkModels = RedactAll(profile.Preferences.WorkModels),
            EmploymentTypes = RedactAll(profile.Preferences.EmploymentTypes),
            SalaryExpectation = profile.Preferences.SalaryExpectation is { } salary
                ? salary with { Currency = RedactOrNull(salary.Currency) }
                : null,
        },
    };

    public string Rehydrate(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        foreach (var (placeholder, value) in _rehydrate)
        {
            text = text.Replace(placeholder, value, StringComparison.Ordinal);
        }

        return text;
    }

    private static IEnumerable<(string Value, string Placeholder)> ExtractCandidates(CandidateProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Name))
        {
            var name = profile.Name.Trim();
            yield return (name, "[NAME]");

            var tokens = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 2)
            {
                yield return (tokens[0], "[FIRST_NAME]");
                yield return (tokens[^1], "[LAST_NAME]");
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.Location))
        {
            yield return (profile.Location.Trim(), "[LOCATION_1]");
        }

        var index = 0;
        foreach (var experience in profile.Experience)
        {
            if (!string.IsNullOrWhiteSpace(experience.Company))
            {
                index++;
                yield return (experience.Company.Trim(), $"[EMPLOYER_{index}]");
            }
        }

        index = 0;
        foreach (var education in profile.Education)
        {
            if (!string.IsNullOrWhiteSpace(education.Institution))
            {
                index++;
                yield return (education.Institution.Trim(), $"[INSTITUTION_{index}]");
            }
        }
    }

    private static string ReplaceToken(string text, string value, string placeholder)
    {
        var result = new StringBuilder(text.Length);
        var index = 0;

        while (index < text.Length)
        {
            var found = text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                result.Append(text, index, text.Length - index);
                break;
            }

            var before = found == 0 ? '\0' : text[found - 1];
            var afterIndex = found + value.Length;
            var after = afterIndex >= text.Length ? '\0' : text[afterIndex];

            if (IsBoundary(before) && IsBoundary(after))
            {
                result.Append(text, index, found - index);
                result.Append(placeholder);
                index = afterIndex;
            }
            else
            {
                result.Append(text, index, found - index + 1);
                index = found + 1;
            }
        }

        return result.ToString();
    }

    private static bool IsBoundary(char c) => !char.IsLetterOrDigit(c);

    private string? RedactOrNull(string? text) => text is null ? null : Redact(text);

    private IReadOnlyList<string> RedactAll(IReadOnlyList<string> items) =>
        items.Select(Redact).ToList();
}