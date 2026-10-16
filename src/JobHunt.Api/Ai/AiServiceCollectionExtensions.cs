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

        var embeddingProvider = AiProviderResolver.ResolveEmbedding(options);
        var embeddingKey = AiProviderKeys.Resolve(AiProviderResolver.EmbeddingProviderName(options), embeddingProvider)[0];

        services.AddChatClient(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var chatLogger = loggerFactory.CreateLogger<FallbackChatClient>();
            var retryLogger = loggerFactory.CreateLogger<RetryingChatClient>();

            var attempts = AiChatPlanner.BuildAttempts(
                options,
                (provider, apiKey) => new RetryingChatClient(
                    AiClientFactory.CreateChatClient(provider, apiKey),
                    new AiRetryPolicy(options.Retry, retryLogger)),
                (providerName, reason) => chatLogger.LogWarning("Skipping AI fallback provider {Provider}: {Reason}", providerName, reason));

            return new FallbackChatClient(attempts, chatLogger);
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
            AiClientFactory.CreateEmbeddingGenerator(embeddingProvider, embeddingKey));

        return services;
    }
}
