using System.Text.Json;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.FollowUps;

public sealed class FollowUpAgent
{
    private const int MaxAttempts = 3;

    private const string Instructions = """
        You are a career coach that drafts a short, polite follow-up message a candidate can send
        after applying for a job, if they have not heard back.

        The candidate profile is provided below as JSON. Personal details appear as placeholders
        such as [NAME] and [EMPLOYER_1]; treat them as the candidate's real values and keep the
        placeholders exactly as they are in your output.

        Candidate profile (JSON):
        __PROFILE_JSON__

        Given the job description the candidate applied to, produce a follow-up draft and return
        ONLY a single JSON object matching this exact schema:
        {
          "subject": "A short, specific email subject line.",
          "body": "The full follow-up message as a single string.",
          "sendAfterDays": 7
        }

        Rules:
        - Output only the JSON object. No prose, no markdown fences, no commentary.
        - subject: concise and specific to the role and company; avoid generic filler.
        - body: a warm, professional nudge of roughly 80 to 150 words. Reference the role and
          company, briefly restate why the candidate is a strong fit, and make a light ask for an
          update or a next step. Never invent details or claim an interview happened.
        - sendAfterDays: a whole number of days after applying that it is reasonable to wait before
          sending (between 3 and 21; 7 is typical).
        - Keep placeholders such as [NAME], [EMPLOYER_1], and [LOCATION_1] exactly as they are.
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AIAgent _agent;
    private readonly ILogger<FollowUpAgent> _logger;

    public FollowUpAgent(
        IChatClient chatClient,
        PiiRedactor redactor,
        CandidateProfile profile,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FollowUpAgent>();

        var profileJson = JsonSerializer.Serialize(redactor.Redact(profile), SerializerOptions);
        var instructions = Instructions.Replace("__PROFILE_JSON__", profileJson, StringComparison.Ordinal);

        _agent = chatClient.AsAIAgent(instructions: instructions, name: "follow_up_agent");
    }

    public async Task<FollowUpDraft> DraftAsync(string jobDescription, CancellationToken ct)
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
                    return FollowUpJson.Parse(text);
                }
                catch (FollowUpFormatException)
                {
                    _logger.LogWarning("FollowUpAgent returned non-JSON output on attempt {Attempt}: {Output}", attempt, text);
                }
            }
            else
            {
                _logger.LogWarning("FollowUpAgent returned an empty response on attempt {Attempt}", attempt);
            }

            userMessage = RetryMessage(jobDescription, text.Length == 0);
        }

        _logger.LogWarning("FollowUpAgent failed after {Attempts} attempts", MaxAttempts);
        throw new FollowUpFormatException("FollowUpAgent failed to return a valid draft.");
    }

    private static string RetryMessage(string jobDescription, bool wasEmpty) =>
        (wasEmpty
            ? "Your previous response was empty."
            : "Your previous response was not valid JSON matching the required schema.") +
        " Reply with a single JSON object containing subject, body and sendAfterDays, and nothing else." +
        " The job description was:\n\n" + jobDescription;
}
