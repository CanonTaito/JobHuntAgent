using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Match;

public static class MatchAgentFactory
{
    public static IMatchAgent Create(
        AiOptions options,
        IChatClient chatClient,
        MatchTools matchTools,
        ILoggerFactory loggerFactory)
    {
        var provider = AiProviderResolver.Resolve(options);

        if (!provider.SupportsTools)
        {
            return new NoToolMatchAgent(chatClient, matchTools, loggerFactory);
        }

        return new MatchAgent(chatClient, matchTools, loggerFactory);
    }
}
