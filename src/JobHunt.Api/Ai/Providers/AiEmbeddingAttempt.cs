using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Providers;

public sealed record AiEmbeddingAttempt(string ProviderName, int KeyIndex, IEmbeddingGenerator<string, Embedding<float>> Generator);
