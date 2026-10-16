using Microsoft.Extensions.AI;

namespace JobHunt.Api.Ai.Providers;

public sealed record AiChatAttempt(string ProviderName, int KeyIndex, IChatClient Client);
