using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Match;

public sealed class MatchAgent
{
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
    private readonly ILogger<MatchAgent> _logger;

    public MatchAgent(IChatClient chatClient, MatchTools matchTools, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MatchAgent>();

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
        for (var attempt = 0; ; attempt++)
        {
            var response = await _agent.RunAsync([new ChatMessage(ChatRole.User, userMessage)], session: null, options: null, ct);
            try
            {
                return MatchScoreJson.Parse(response.Text.Trim());
            }
            catch (MatchScoreFormatException)
            {
                _logger.LogWarning("MatchAgent output was not parseable on attempt {Attempt}: {Output}", attempt, response.Text);

                if (attempt != 0)
                {
                    throw;
                }

                userMessage =
                    "Your previous response was not valid JSON matching the required schema. " +
                    "Call ScoreJob again and reply with only the JSON object. The job description was:\n\n" + jobDescription;
            }
        }
    }
}
