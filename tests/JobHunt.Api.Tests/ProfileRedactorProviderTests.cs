using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class ProfileRedactorProviderTests
{
    [Fact]
    public void Redacts_the_current_profile()
    {
        var store = new FakeProfileStore(new CandidateProfile
        {
            Name = "Canon Taito",
            Location = "Brisbane",
        });
        var provider = new ProfileRedactorProvider(store, NullLoggerFactory.Instance);

        var redacted = provider.RedactCurrent();

        Assert.Equal("[NAME]", redacted.Name);
        Assert.Equal("[LOCATION_1]", redacted.Location);
    }

    [Fact]
    public void Rebuilds_against_the_profile_in_effect()
    {
        var store = new FakeProfileStore(new CandidateProfile { Name = "First Person" });
        var provider = new ProfileRedactorProvider(store, NullLoggerFactory.Instance);

        store.Current = new CandidateProfile
        {
            Name = "Second Person",
            Experience = [new ExperienceEntry { Company = "NewCo" }],
        };

        var redacted = provider.RedactCurrent();

        Assert.Equal("[NAME]", redacted.Name);
        Assert.Equal("[EMPLOYER_1]", redacted.Experience[0].Company);
    }
}
