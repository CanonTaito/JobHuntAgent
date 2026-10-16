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
        AiProviderKeys.Resolve(AiProviderResolver.EmbeddingProviderName(options), embeddingProvider);

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

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var embeddingLogger = loggerFactory.CreateLogger<FallbackEmbeddingGenerator>();
            var retryLogger = loggerFactory.CreateLogger<RetryingEmbeddingGenerator>();

            var attempts = AiEmbeddingPlanner.BuildAttempts(
                options,
                (provider, apiKey) => new RetryingEmbeddingGenerator(
                    AiClientFactory.CreateEmbeddingGenerator(provider, apiKey),
                    new AiRetryPolicy(options.Retry, retryLogger)),
                (providerName, reason) => embeddingLogger.LogWarning("Skipping AI embedding fallback provider {Provider}: {Reason}", providerName, reason));

            return new FallbackEmbeddingGenerator(attempts, embeddingLogger);
        });

        return services;
    }
}
