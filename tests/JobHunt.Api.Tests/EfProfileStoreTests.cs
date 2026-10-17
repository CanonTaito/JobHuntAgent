using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class EfProfileStoreTests : IDisposable
{
    private static readonly CandidateProfile Fallback = new()
    {
        Name = "Fallback Person",
        Headline = "From file",
    };

    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public void Returns_the_fallback_when_nothing_is_stored()
    {
        var store = CreateStore();

        Assert.Equal("Fallback Person", store.Current.Name);
    }

    [Fact]
    public async Task Saves_and_reads_back_a_profile()
    {
        var store = CreateStore();
        var profile = new CandidateProfile
        {
            Name = "Uploaded Person",
            Headline = "Parsed from resume",
            Skills = ["C#", ".NET"],
        };

        await store.SaveAsync(profile);

        Assert.Equal("Uploaded Person", store.Current.Name);
        Assert.Contains("C#", store.Current.Skills);

        var reloaded = CreateStore();
        Assert.Equal("Uploaded Person", reloaded.Current.Name);
        Assert.Equal("Parsed from resume", reloaded.Current.Headline);
    }

    [Fact]
    public async Task Overwrites_the_existing_profile()
    {
        var store = CreateStore();

        await store.SaveAsync(new CandidateProfile { Name = "First" });
        await store.SaveAsync(new CandidateProfile { Name = "Second" });

        Assert.Equal("Second", store.Current.Name);
        Assert.Equal("Second", CreateStore().Current.Name);
    }

    [Fact]
    public async Task GetAsync_returns_the_current_profile()
    {
        var store = CreateStore();
        await store.SaveAsync(new CandidateProfile { Name = "Uploaded Person" });

        Assert.Equal("Uploaded Person", (await store.GetAsync()).Name);
    }

    [Fact]
    public void Falls_back_when_the_stored_profile_is_corrupt()
    {
        using (var db = _factory.CreateDbContext())
        {
            db.Profiles.Add(new ProfileRecord
            {
                Id = 1,
                Json = "{ not valid json",
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            db.SaveChanges();
        }

        var store = CreateStore();

        Assert.Equal("Fallback Person", store.Current.Name);
    }

    private EfProfileStore CreateStore() =>
        new(_factory, Fallback, NullLogger<EfProfileStore>.Instance);

    public void Dispose() => _factory.Dispose();
}
