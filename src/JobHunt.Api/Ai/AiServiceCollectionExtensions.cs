using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Ai.Retry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Ai;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddJobHuntAi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        var chatProvider = AiProviderResolver.Resolve(options);
        var chatKeys = AiProviderKeys.Resolve(options.Provider, chatProvider);

        var embeddingProvider = AiProviderResolver.ResolveEmbedding(options);
        var embeddingKey = AiProviderKeys.Resolve(AiProviderResolver.EmbeddingProviderName(options), embeddingProvider)[0];

        services.AddChatClient(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var attempts = chatKeys
                .Select((key, index) => new AiChatAttempt(
                    options.Provider,
                    index,
                    new RetryingChatClient(
                        AiClientFactory.CreateChatClient(chatProvider, key),
                        new AiRetryPolicy(options.Retry, loggerFactory.CreateLogger<RetryingChatClient>()))))
                .ToList();

            return new FallbackChatClient(attempts, loggerFactory.CreateLogger<FallbackChatClient>());
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
            AiClientFactory.CreateEmbeddingGenerator(embeddingProvider, embeddingKey));

        return services;
    }
}
