using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Resume;

public sealed class ResumeParserAgent
{
    private const int MaxAttempts = 3;

    private const string Instructions = """
        You are a resume parser. Given the text of a candidate's resume, extract a structured profile and return ONLY a single JSON object matching this exact schema:
        {
          "name": string,
          "headline": string or null,
          "location": string or null,
          "yearsOfExperience": number or null,
          "seniority": string or null,
          "summary": string or null,
          "skills": [string],
          "experience": [ { "company": string, "title": string, "period": string, "highlights": [string] } ],
          "education": [ { "qualification": string, "institution": string, "year": number or null } ],
          "highlights": [string],
          "preferences": { "roles": [string], "locations": [string], "workModels": [string], "employmentTypes": [string] }
        }

        Rules:
        - Output only the JSON object. No prose, no markdown fences, no commentary.
        - Keep any [PLACEHOLDER] token exactly as written; never expand, translate, or invent a value for it.
        - Use null or an empty array for anything the resume does not state; never guess.
        - skills: concise technology and skill names.
        - highlights: the strongest one-line achievements, kept close to the resume's wording.
        """;

    private const string RetryMessage =
        "Your previous response was not valid JSON matching the required schema. Return only the JSON object.";

    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ResumeParserAgent> _logger;

    public ResumeParserAgent(IChatClient chatClient, ILoggerFactory loggerFactory)
    {
        _chatClient = chatClient;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ResumeParserAgent>();
    }

    public async Task<CandidateProfile> ParseAsync(
        string resumeText,
        string candidateName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resumeText);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateName);

        var redactor = new PiiRedactor(
            new CandidateProfile { Name = candidateName },
            new InMemoryPiiMappingStore(),
            _loggerFactory.CreateLogger<PiiRedactor>());

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, Instructions),
            new(ChatRole.User, redactor.Redact(resumeText)),
        };

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var response = await _chatClient.GetResponseAsync(messages, options: null, ct);
            var text = response.Text?.Trim() ?? string.Empty;

            if (text.Length > 0)
            {
                try
                {
                    return ResumeJsonParser.Parse(text) with { Name = candidateName };
                }
                catch (ResumeParseFormatException)
                {
                    _logger.LogWarning("Resume parser returned non-JSON output on attempt {Attempt}: {Output}", attempt, text);
                }
            }
            else
            {
                _logger.LogWarning("Resume parser returned an empty response on attempt {Attempt}", attempt);
            }

            messages.Add(new ChatMessage(ChatRole.Assistant, text));
            messages.Add(new ChatMessage(ChatRole.User, RetryMessage));
        }

        throw new ResumeParseFormatException(
            $"The resume could not be parsed into a profile after {MaxAttempts} attempts.");
    }
}
