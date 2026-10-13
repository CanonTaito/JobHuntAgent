namespace JobHunt.Api.Features.FollowUps;

public enum FollowUpResultStatus
{
    Succeeded,
    NotFound,
    NotApplied,
}

public sealed record FollowUpResult(FollowUpResultStatus Status, FollowUpDraft? Draft, string? Message);
