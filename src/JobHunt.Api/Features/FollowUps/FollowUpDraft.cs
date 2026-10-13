namespace JobHunt.Api.Features.FollowUps;

public sealed record FollowUpDraft
{
    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public int SendAfterDays { get; init; }
}
