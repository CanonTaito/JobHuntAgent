namespace JobHunt.Api.Ai.Retry;

public sealed class AiRetryOptions
{
    public int MaxAttempts { get; set; } = 3;

    public int BaseDelayMs { get; set; } = 500;

    public int MaxDelayMs { get; set; } = 8000;
}
