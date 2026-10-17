using JobHunt.Api.Ai;
using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class MatchAgentFactoryTests
{
    [Fact]
    public void Picks_the_prompt_only_agent_when_tool_support_is_disabled()
    {
        var options = new AiOptions
        {
            Provider = "ollama",
            Providers =
            {
                ["ollama"] = new ProviderOptions
                {
                    Endpoint = "http://localhost:11434/v1",
                    ChatModel = "gemma3",
                    SupportsTools = false,
                },
            },
        };

        var chat = new FakeChatClient(new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]));

        var agent = MatchAgentFactory.Create(options, chat, CreateTools(chat), NullLoggerFactory.Instance);

        Assert.IsType<NoToolMatchAgent>(agent);
    }

    [Fact]
    public void Picks_the_tool_agent_by_default()
    {
        var options = new AiOptions
        {
            Provider = "ollama",
            Providers =
            {
                ["ollama"] = new ProviderOptions
                {
                    Endpoint = "http://localhost:11434/v1",
                    ChatModel = "gemma3",
                },
            },
        };

        var chat = new FakeChatClient(new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]));

        var agent = MatchAgentFactory.Create(options, chat, CreateTools(chat), NullLoggerFactory.Instance);

        Assert.IsType<MatchAgent>(agent);
    }

    private static MatchTools CreateTools(IChatClient chat)
    {
        var profile = new CandidateProfile { Name = "Test", Skills = ["alpha"] };
        var redactors = new ProfileRedactorProvider(new FakeProfileStore(profile), NullLoggerFactory.Instance);

        return new MatchTools(redactors, chat);
    }
}
