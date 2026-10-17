namespace JobHunt.Api.Features.Profile;

public interface IProfileStore
{
    CandidateProfile Current { get; }

    Task<CandidateProfile> GetAsync(CancellationToken ct = default);

    Task SaveAsync(CandidateProfile profile, CancellationToken ct = default);
}
