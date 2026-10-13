using System.Text.Json;

namespace JobHunt.Api.Features.FollowUps;

public static class FollowUpJson
{
    public const int MinSendAfterDays = 3;
    public const int MaxSendAfterDays = 21;
    public const int DefaultSendAfterDays = 7;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static FollowUpDraft Parse(string raw)
    {
        var json = ExtractJson(raw);

        RawFollowUpDraft? result;
        try
        {
            result = JsonSerializer.Deserialize<RawFollowUpDraft>(json, Options);
        }
        catch (JsonException e)
        {
            throw new FollowUpFormatException("Model output was not valid JSON.", e);
        }

        if (result is null)
        {
            throw new FollowUpFormatException("Model output was not a JSON object.");
        }

        var subject = result.Subject?.Trim();
        var body = result.Body?.Trim();
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
        {
            throw new FollowUpFormatException("Model output was missing the subject or body.");
        }

        return new FollowUpDraft
        {
            Subject = subject,
            Body = body,
            SendAfterDays = ClampDays(result.SendAfterDays),
        };
    }

    private static int ClampDays(int? days) =>
        days is null ? DefaultSendAfterDays : Math.Clamp(days.Value, MinSendAfterDays, MaxSendAfterDays);

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new FollowUpFormatException("No JSON object found in model output.");
        }

        return raw[start..(end + 1)];
    }

    private sealed class RawFollowUpDraft
    {
        public string? Subject { get; init; }

        public string? Body { get; init; }

        public int? SendAfterDays { get; init; }
    }
}
