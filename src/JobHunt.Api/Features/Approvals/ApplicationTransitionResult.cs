using JobHunt.Api.Features.Applications;

namespace JobHunt.Api.Features.Approvals;

public enum TransitionOutcome
{
    Succeeded,
    NotFound,
    InvalidState,
}

public sealed record ApplicationTransitionResult(
    TransitionOutcome Outcome,
    ApplicationView? Application,
    string? Message);
