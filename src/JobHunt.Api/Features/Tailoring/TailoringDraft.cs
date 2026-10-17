namespace JobHunt.Api.Features.Tailoring;

public sealed record TailoringDraft
{
    public IReadOnlyList<string> ResumeBullets { get; init; } = [];

    public string CoverLetter { get; init; } = string.Empty;

    public JobHunt.Api.Features.Profile.CandidateProfile? TailoredProfile { get; init; }
}
