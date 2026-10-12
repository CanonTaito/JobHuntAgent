namespace JobHunt.Api.Features.Outcomes;

public enum OutcomeResultStatus
{
    Succeeded,
    NotFound,
    NotSubmitted,
}

public sealed record OutcomeResult(OutcomeResultStatus Status, OutcomeView? Record, string? Message);
