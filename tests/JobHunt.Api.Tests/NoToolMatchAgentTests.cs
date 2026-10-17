using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class NoToolMatchAgentTests
{
    private const string ValidScore = """{"skills":50,"seniority":60,"fit":70,"summary":"ok","strengths":["a"],"gaps":["b"]}""";

    [Fact]
    public async Task Parses_a_valid_json_reply()
    {
        var chat = new ScriptedChatClient(ValidScore);
        var agent = CreateAgent(chat);

        var score = await agent.ScoreAsync("job description", CancellationToken.None);

        Assert.Equal(50, score.Skills);
        Assert.Equal(60, score.Seniority);
        Assert.Equal(70, score.Fit);
        Assert.Equal("ok", score.Summary);
        Assert.Equal(1, chat.Calls);
    }

    [Fact]
    public async Task Retries_after_an_invalid_reply()
    {
        var chat = new ScriptedChatClient("not json", ValidScore);
        var agent = CreateAgent(chat);

        var score = await agent.ScoreAsync("job description", CancellationToken.None);

        Assert.Equal(50, score.Skills);
        Assert.Equal(2, chat.Calls);
    }

    [Fact]
    public async Task Falls_back_to_the_score_tool_after_three_invalid_replies()
    {
        var chat = new ScriptedChatClient("not json", "still not json", "nope", ValidScore);
        var agent = CreateAgent(chat);

        var score = await agent.ScoreAsync("job description", CancellationToken.None);

        Assert.Equal(50, score.Skills);
        Assert.Equal(4, chat.Calls);
    }

    private static NoToolMatchAgent CreateAgent(ScriptedChatClient chat)
    {
        var profile = new CandidateProfile { Name = "Test", Skills = ["alpha"] };
        var redactors = new ProfileRedactorProvider(new FakeProfileStore(profile), NullLoggerFactory.Instance);
        var tools = new MatchTools(redactors, chat);

        return new NoToolMatchAgent(chat, tools, NullLoggerFactory.Instance);
    }
}
