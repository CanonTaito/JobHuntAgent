using JobHunt.Api.Features.Resume;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class ResumeParserAgentTests
{
    private const string Name = "Canon Taito";

    private const string ValidProfile =
        """{"name":"[NAME]","headline":"Senior Software Engineer","skills":["C#"]}""";

    [Fact]
    public async Task Parses_a_profile_and_keeps_the_provided_name()
    {
        var chat = new ScriptedChatClient(ValidProfile);
        var agent = new ResumeParserAgent(chat, NullLoggerFactory.Instance);

        var profile = await agent.ParseAsync("resume text", Name);

        Assert.Equal(Name, profile.Name);
        Assert.Equal("Senior Software Engineer", profile.Headline);
        Assert.Equal(1, chat.Calls);
    }

    [Fact]
    public async Task Redacts_the_name_and_contacts_before_calling_the_model()
    {
        var chat = new ScriptedChatClient(ValidProfile);
        var agent = new ResumeParserAgent(chat, NullLoggerFactory.Instance);
        var resume = "Canon Taito\ncanon.taito@example.com\n0487 100 384\nSenior Software Engineer";

        await agent.ParseAsync(resume, Name);

        var userMessage = chat.LastMessages.Single(message => message.Role == ChatRole.User).Text;
        Assert.DoesNotContain("Canon", userMessage);
        Assert.DoesNotContain("canon.taito@example.com", userMessage);
        Assert.DoesNotContain("0487 100 384", userMessage);
        Assert.Contains("[NAME]", userMessage);
        Assert.Contains("[EMAIL]", userMessage);
        Assert.Contains("[PHONE]", userMessage);
    }

    [Fact]
    public async Task Retries_after_an_invalid_reply()
    {
        var chat = new ScriptedChatClient("not json", ValidProfile);
        var agent = new ResumeParserAgent(chat, NullLoggerFactory.Instance);

        var profile = await agent.ParseAsync("resume text", Name);

        Assert.Equal("Senior Software Engineer", profile.Headline);
        Assert.Equal(2, chat.Calls);
    }

    [Fact]
    public async Task Throws_after_the_maximum_attempts()
    {
        var chat = new ScriptedChatClient("not json", "still not json", "nope");
        var agent = new ResumeParserAgent(chat, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ResumeParseFormatException>(
            () => agent.ParseAsync("resume text", Name));

        Assert.Equal(3, chat.Calls);
    }
}
