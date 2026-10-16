using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Match;

public sealed class NoToolMatchAgent : IMatchAgent
{
    private const int MaxAttempts = 3;

    private const string Instructions = """
        You are a job match analyst.

        Steps:
        1. Analyze the job description against the candidate profile.
        2. Return exactly one JSON object with this schema:
           {
             "skills": number,
             "seniority": number,
             "fit": number,
             "summary": string,
             "strengths": string[],
             "gaps": string[]
           }
        All scores are integers 0-100. Never include prose before or after.
        """;

    private readonly IChatClient _chatClient;
    private readonly MatchTools _tools;
    private readonly ILogger<NoToolMatchAgent> _logger;

    public NoToolMatchAgent(IChatClient chatClient, MatchTools matchTools, ILoggerFactory loggerFactory)
    {
        _chatClient = chatClient;
        _tools = matchTools;
        _logger = loggerFactory.CreateLogger<NoToolMatchAgent>();
    }

    public async Task<MatchScore> ScoreAsync(string jobDescription, CancellationToken ct)
    {
        var profile = await _tools.GetProfileAsync(ct);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, Instructions),
            new(ChatRole.User, $"Candidate profile (redacted): {profile}\n\nJob description:\n{jobDescription}"),
        };

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var response = await _chatClient.GetResponseAsync(messages, options: null, ct);
            var text = response.Text?.Trim() ?? string.Empty;

            if (text.Length > 0)
            {
                try
                {
                    return MatchScoreJson.Parse(text);
                }
                catch (MatchScoreFormatException)
                {
                    _logger.LogWarning("NoToolMatchAgent returned non-JSON output on attempt {Attempt}: {Output}", attempt, text);
                }
            }
            else
            {
                _logger.LogWarning("NoToolMatchAgent returned an empty response on attempt {Attempt}", attempt);
            }

            messages.Add(new ChatMessage(ChatRole.Assistant, text));
            messages.Add(new ChatMessage(ChatRole.User, RetryMessage(jobDescription)));
        }

        _logger.LogWarning("NoToolMatchAgent failed after {Attempts} attempts; scoring with the ScoreJob tool directly.", MaxAttempts);
        return MatchScoreJson.Parse(await _tools.ScoreJobAsync(jobDescription, ct));
    }

    private static string RetryMessage(string jobDescription) =>
        "Your previous response was not valid JSON matching the required schema. " +
        "Return only the JSON object. The job description was:\n\n" + jobDescription;
}
