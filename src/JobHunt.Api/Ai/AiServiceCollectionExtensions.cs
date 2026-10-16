using System.ClientModel;
using System.ClientModel.Primitives;
using JobHunt.Api.Ai.Providers;
using JobHunt.Api.Ai.Retry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace JobHunt.Api.Ai;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddJobHuntAi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        var chatProvider = AiProviderResolver.Resolve(options);
        var embeddingProvider = AiProviderResolver.ResolveEmbedding(options);

        services.AddChatClient(serviceProvider =>
        {
            var policy = new AiRetryPolicy(
                options.Retry,
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RetryingChatClient>());

            return new RetryingChatClient(CreateChatClient(chatProvider), policy);
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => CreateEmbeddingGenerator(embeddingProvider));

        return services;
    }

    private static IChatClient CreateChatClient(ProviderOptions provider) =>
        CreateClient(provider).GetChatClient(provider.ChatModel).AsIChatClient();

    private static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(ProviderOptions provider) =>
        CreateClient(provider).GetEmbeddingClient(provider.EmbeddingModel).AsIEmbeddingGenerator(null);

    private static OpenAIClient CreateClient(ProviderOptions provider) =>
        new(
            new ApiKeyCredential(provider.EffectiveApiKeys.FirstOrDefault() ?? string.Empty),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(provider.Endpoint),
                RetryPolicy = new ClientRetryPolicy(0),
            });
}
