using System.Text.Json;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Tailoring;

public sealed class TailoringAgent
{
    private const int MaxAttempts = 3;

    private const string Instructions = """
        You are a career coach that tailors application material for one specific job.

        The candidate profile is provided below as JSON. Personal details appear as placeholders
        such as [NAME] and [EMPLOYER_1]; treat them as the candidate's real values and keep the
        placeholders exactly as they are in your output.

        Candidate profile (JSON):
        __PROFILE_JSON__

        Given a job description, produce a tailored draft and return ONLY a single JSON object
        matching this exact schema:
        {
          "resumeBullets": ["First tailored bullet.", "Second tailored bullet."],
          "coverLetter": "The full cover letter as a single string."
        }

        Rules:
        - Output only the JSON object. No prose, no markdown fences, no commentary.
        - resumeBullets: 4 to 6 bullets, each a single line of plain text. Select and rephrase the
          candidate's most relevant experience and highlights for THIS job, mirroring the job's
          language and emphasising overlapping skills. Quantify impact only where the profile
          supports it.
        - coverLetter: a complete, ready-to-send cover letter of roughly 200 to 300 words, in the
          candidate's voice. Reference the specific role, company, and the strongest matches between
          the candidate's experience and the job.
        - Never invent experience, employers, or qualifications the profile does not contain.
        - Keep placeholders such as [NAME], [EMPLOYER_1], and [LOCATION_1] exactly as they are.
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AIAgent _agent;
    private readonly ILogger<TailoringAgent> _logger;

    public TailoringAgent(
        IChatClient chatClient,
        PiiRedactor redactor,
        CandidateProfile profile,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TailoringAgent>();

        var profileJson = JsonSerializer.Serialize(redactor.Redact(profile), SerializerOptions);
        var instructions = Instructions.Replace("__PROFILE_JSON__", profileJson, StringComparison.Ordinal);

        _agent = chatClient.AsAIAgent(instructions: instructions, name: "tailoring_agent");
    }

    public async Task<TailoringDraft> DraftAsync(string jobDescription, CancellationToken ct)
    {
        var userMessage = jobDescription;
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var response = await _agent.RunAsync([new ChatMessage(ChatRole.User, userMessage)], session: null, options: null, ct);
            var text = response.Text?.Trim() ?? string.Empty;

            if (text.Length > 0)
            {
                try
                {
                    return TailoringJson.Parse(text);
                }
                catch (TailoringFormatException)
                {
                    _logger.LogWarning("TailoringAgent returned non-JSON output on attempt {Attempt}: {Output}", attempt, text);
                }
            }
            else
            {
                _logger.LogWarning("TailoringAgent returned an empty response on attempt {Attempt}", attempt);
            }

            userMessage = RetryMessage(jobDescription, text.Length == 0);
        }

        _logger.LogWarning("TailoringAgent failed after {Attempts} attempts", MaxAttempts);
        throw new TailoringFormatException("TailoringAgent failed to return a valid draft.");
    }

    private static string RetryMessage(string jobDescription, bool wasEmpty) =>
        (wasEmpty
            ? "Your previous response was empty."
            : "Your previous response was not valid JSON matching the required schema.") +
        " Reply with a single JSON object containing resumeBullets and coverLetter, and nothing else." +
        " The job description was:\n\n" + jobDescription;
}
