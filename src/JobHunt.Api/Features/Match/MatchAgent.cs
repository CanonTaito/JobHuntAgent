using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Match;

public sealed class MatchAgent
{
    private const int MaxAttempts = 3;

    private const string Instructions = """
        You are a job match analyst. You have two tools: GetProfile and ScoreJob.

        Steps:
        1. Call GetProfile to load the candidate profile.
        2. Call ScoreJob with the job description.
        3. Reply with the JSON object returned by ScoreJob.

        Rules:
        - Your reply must be exactly one JSON object — the ScoreJob result verbatim.
        - No prose before or after. No explanations. No markdown fences.
        - Never invent a score yourself; always use ScoreJob's result.
        """;

    private readonly AIAgent _agent;
    private readonly MatchTools _tools;
    private readonly ILogger<MatchAgent> _logger;

    public MatchAgent(IChatClient chatClient, MatchTools matchTools, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MatchAgent>();
        _tools = matchTools;

        AITool[] tools =
        [
            AIFunctionFactory.Create(
                matchTools.GetProfile,
                name: "GetProfile",
                description: "Returns the candidate profile as JSON, with personal details replaced by placeholders."),
            AIFunctionFactory.Create(
                matchTools.ScoreJobAsync,
                name: "ScoreJob",
                description: "Scores how well a job description matches the candidate profile. Returns a JSON object with skills, seniority, fit, summary, strengths, and gaps."),
        ];

        var client = chatClient.AsBuilder().UseFunctionInvocation(loggerFactory).Build();
        _agent = client.AsAIAgent(
            instructions: Instructions,
            name: "match_agent",
            tools: tools,
            loggerFactory: loggerFactory);
    }

    public async Task<MatchScore> ScoreAsync(string jobDescription, CancellationToken ct)
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
                    return MatchScoreJson.Parse(text);
                }
                catch (MatchScoreFormatException)
                {
                    _logger.LogWarning("MatchAgent returned non-JSON output on attempt {Attempt}: {Output}", attempt, text);
                }
            }
            else
            {
                _logger.LogWarning("MatchAgent returned an empty response on attempt {Attempt}", attempt);
            }

            userMessage = RetryMessage(jobDescription, text.Length == 0);
        }

        _logger.LogWarning("MatchAgent failed after {Attempts} attempts; scoring with the ScoreJob tool directly.", MaxAttempts);
        return MatchScoreJson.Parse(await _tools.ScoreJobAsync(jobDescription, ct));
    }

    private static string RetryMessage(string jobDescription, bool wasEmpty) =>
        (wasEmpty
            ? "Your previous response was empty."
            : "Your previous response was not valid JSON matching the required schema.") +
        " Call GetProfile, then ScoreJob with the job description, and reply with only the JSON object ScoreJob returned." +
        " The job description was:\n\n" + jobDescription;
}
