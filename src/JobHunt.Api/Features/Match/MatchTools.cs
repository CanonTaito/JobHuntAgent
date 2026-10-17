using System.Text.Json;
using JobHunt.Api.Features.Pii;
using Microsoft.Extensions.AI;

namespace JobHunt.Api.Features.Match;

public sealed class MatchTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient;
    private readonly ProfileRedactorProvider _redactors;

    public MatchTools(ProfileRedactorProvider redactors, IChatClient chatClient)
    {
        _redactors = redactors;
        _chatClient = chatClient;
    }

    public string GetProfile() => BuildProfile();

    public Task<string> GetProfileAsync(CancellationToken ct = default) => Task.FromResult(BuildProfile());

    public async Task<string> ScoreJobAsync(string jobDescription, CancellationToken ct)
    {
        var userMessage = BuildPrompt(jobDescription);
        for (var attempt = 0; ; attempt++)
        {
            var response = await _chatClient.GetResponseAsync(userMessage, options: null, ct);
            try
            {
                return JsonSerializer.Serialize(MatchScoreJson.Parse(response.Text), SerializerOptions);
            }
            catch (MatchScoreFormatException)
            {
                if (attempt != 0)
                {
                    throw;
                }

                userMessage = BuildPrompt(jobDescription) +
                    "\n\nYour previous response was not valid JSON matching the required schema. " +
                    "Try again, outputting only the JSON object.";
            }
        }
    }

    private string BuildProfile() =>
        JsonSerializer.Serialize(_redactors.RedactCurrent(), SerializerOptions);

    private string BuildPrompt(string jobDescription) => $$"""
        Compare this candidate profile against the job description and score the match.

        Candidate profile (JSON):
        {{BuildProfile()}}

        Job description:
        {{jobDescription}}

        Return ONLY a single JSON object, in exactly this shape:
        {
          "skills": 85,
          "seniority": 80,
          "fit": 75,
          "summary": "One or two sentences summarising the match.",
          "strengths": ["First reason the candidate fits.", "Second reason.", "Third reason."],
          "gaps": ["A shortcoming or risk."]
        }

        Rules:
        - Output only the JSON object. No prose, no markdown fences, no commentary.
        - "skills", "seniority", and "fit" are integers from 0 to 100.
        - "strengths" and "gaps" are arrays of plain text strings — never objects, never key/value pairs.
        - Placeholders such as [NAME] or [EMPLOYER_1] are anonymised values; keep them as they are.
        """;
}
