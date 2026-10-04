using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace JobHunt.Api.Features.JdScan;

public sealed class JdScannerAgent
{
    private const string Instructions = """
        You are a senior recruitment analyst. Given a raw job description, extract the facts from it and return ONLY a single JSON object matching this exact schema:
        {
          "title": string,
          "company": string or null,
          "location": string or null,
          "employmentType": "Full-time" | "Part-time" | "Contract" | "Temporary" | "Internship" | "Unknown",
          "remote": "On-site" | "Hybrid" | "Remote" | "Unknown",
          "seniority": "Entry" | "Mid" | "Senior" | "Lead" | "Principal" | "Director",
          "salaryText": string or null,
          "requiredSkills": [string],
          "niceToHaveSkills": [string],
          "responsibilities": [string],
          "benefits": [string],
          "summary": string,
          "keywords": [string]
        }

        Rules:
        - Output only the JSON object. No prose, no markdown fences, no commentary.
        - Use null / "Unknown" when the ad does not state the value; never guess a salary.
        - summary: one or two sentences capturing the role in a hiring manager's words.
        - keywords: 5-10 search-friendly terms covering the role, seniority, and tech stack.
        """;

    private readonly AIAgent _agent;

    public JdScannerAgent(IChatClient chatClient)
    {
        _agent = chatClient.AsAIAgent(instructions: Instructions, name: "jd_scanner");
    }

    public async Task<JdAnalysis> ScanAsync(string rawJd, CancellationToken ct)
    {
        var userMessage = rawJd;
        for (var attempt = 0; ; attempt++)
        {
            var response = await _agent.RunAsync([new ChatMessage(ChatRole.User, userMessage)], session: null, options: null, ct);
            try
            {
                return JdJsonParser.Parse(response.Text.Trim());
            }
            catch (JdScanFormatException)
            {
                if (attempt != 0)
                {
                    throw;
                }

                userMessage =
                    "Your previous response was not valid JSON matching the required schema. " +
                    "Try again, outputting only the JSON object. The job description was:\n\n" + rawJd;
            }
        }
    }
}