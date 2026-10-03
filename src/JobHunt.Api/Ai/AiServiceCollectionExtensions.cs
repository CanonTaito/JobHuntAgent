using System.ClientModel;
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

        services.AddChatClient(_ => options.Provider.Equals("zen", StringComparison.OrdinalIgnoreCase)
            ? CreateZenChatClient(options)
            : CreateOllamaChatClient(options));

        return services;
    }

    private static IChatClient CreateZenChatClient(AiOptions options)
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(options.Zen.ApiKey ?? string.Empty),
            new OpenAIClientOptions { Endpoint = new Uri(options.Zen.Endpoint) });

        return client.GetChatClient(options.Zen.ChatModel).AsIChatClient();
    }

    private static IChatClient CreateOllamaChatClient(AiOptions options)
    {
        var client = new OpenAIClient(
            new ApiKeyCredential("ollama"),
            new OpenAIClientOptions { Endpoint = new Uri(options.Ollama.Endpoint) });

        return client.GetChatClient(options.Ollama.ChatModel).AsIChatClient();
    }
}