using System.ClientModel;
using JobHunt.Api.Ai.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace JobHunt.Api.Ai;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddJobHuntAi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        var provider = AiProviderResolver.Resolve(options);

        services.AddChatClient(_ => CreateChatClient(provider));
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => CreateEmbeddingGenerator(provider));

        return services;
    }

    private static IChatClient CreateChatClient(ProviderOptions provider) =>
        CreateClient(provider).GetChatClient(provider.ChatModel).AsIChatClient();

    private static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(ProviderOptions provider) =>
        CreateClient(provider).GetEmbeddingClient(provider.EmbeddingModel).AsIEmbeddingGenerator(null);

    private static OpenAIClient CreateClient(ProviderOptions provider) =>
        new(
            new ApiKeyCredential(provider.ApiKey ?? string.Empty),
            new OpenAIClientOptions { Endpoint = new Uri(provider.Endpoint) });
}
