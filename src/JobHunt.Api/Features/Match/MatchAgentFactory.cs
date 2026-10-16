using JobHunt.Api.Ai;
using JobHunt.Api.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Match;

public static class MatchAgentFactory
{
    public static IMatchAgent Create(
        IServiceProvider serviceProvider,
        AiOptions options,
        IChatClient chatClient,
        MatchTools matchTools,
        ILoggerFactory loggerFactory)
    {
        var providerName = options.Provider;
        var providerOptions = AiProviderResolver.Resolve(options);
        var supportsTools = AiProviderToolSupport.SupportsTools(options, providerName, providerOptions);

        if (!supportsTools)
        {
            return new NoToolMatchAgent(chatClient, matchTools, loggerFactory);
        }

        return new MatchAgent(chatClient, matchTools, loggerFactory);
    }
}
