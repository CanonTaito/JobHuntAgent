using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.AI;

namespace JobHunt.Api.Features.Match;

public sealed class EmbeddingScorer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly string _profileText;

    public EmbeddingScorer(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        PiiRedactor redactor,
        CandidateProfile profile)
    {
        _generator = generator;
        _profileText = BuildProfileText(redactor.Redact(profile));
    }

    public async Task<int> ScoreAsync(string jobDescription, CancellationToken ct)
    {
        var profileEmbedding = await _generator.GenerateAsync(_profileText, options: null, ct);
        var jobEmbedding = await _generator.GenerateAsync(jobDescription, options: null, ct);
        var similarity = CosineSimilarity(profileEmbedding.Vector.Span, jobEmbedding.Vector.Span);
        return Math.Clamp((int)Math.Round(similarity * 100), 0, 100);
    }

    private static string BuildProfileText(CandidateProfile profile)
    {
        var parts = new List<string>();

        AddIfPresent(parts, profile.Headline);
        AddIfPresent(parts, $"{profile.Seniority ?? "Unknown"} level, {profile.YearsOfExperience ?? 0} years experience");
        AddIfPresent(parts, profile.Summary);
        parts.AddRange(profile.Skills);

        foreach (var entry in profile.Experience)
        {
            AddIfPresent(parts, entry.Title);
            parts.AddRange(entry.Highlights);
        }

        foreach (var entry in profile.Education)
        {
            AddIfPresent(parts, entry.Qualification);
        }

        parts.AddRange(profile.Preferences.Roles);
        parts.AddRange(profile.Preferences.Locations);
        parts.AddRange(profile.Preferences.WorkModels);
        parts.AddRange(profile.Preferences.EmploymentTypes);

        return string.Join('\n', parts);
    }

    private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var length = Math.Min(a.Length, b.Length);
        var dot = 0f;
        var normA = 0f;
        var normB = 0f;
        for (var i = 0; i < length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private static void AddIfPresent(List<string> parts, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(text.Trim());
        }
    }
}
