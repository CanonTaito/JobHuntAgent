using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;

namespace JobHunt.Api.Ai.Providers;

public static class AiClientFactory
{
    public static IChatClient CreateChatClient(ProviderOptions provider, string apiKey) =>
        CreateOpenAIClient(provider, apiKey)
            .GetChatClient(provider.ChatModel)
            .AsIChatClient();

    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(ProviderOptions provider, string apiKey) =>
        CreateOpenAIClient(provider, apiKey)
            .GetEmbeddingClient(provider.EmbeddingModel)
            .AsIEmbeddingGenerator(null);

    private static OpenAIClient CreateOpenAIClient(ProviderOptions provider, string apiKey) =>
        new(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(provider.Endpoint),
                RetryPolicy = new ClientRetryPolicy(0),
            });
}
