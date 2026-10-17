using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Tests.TestSupport;

public sealed class FakeProfileStore(CandidateProfile profile) : IProfileStore
{
    public CandidateProfile Current { get; set; } = profile;

    public Task<CandidateProfile> GetAsync(CancellationToken ct = default) => Task.FromResult(Current);

    public Task SaveAsync(CandidateProfile profile, CancellationToken ct = default)
    {
        Current = profile;
        return Task.CompletedTask;
    }
}
