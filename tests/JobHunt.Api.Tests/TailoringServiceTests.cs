using System.Text.Json;
using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Tailoring;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class TailoringServiceTests
{
    [Fact]
    public async Task Persists_a_tailored_profile_derived_from_the_base_profile()
    {
        using var dbFactory = new TestDbContextFactory();
        var applicationId = await SeedApplicationAsync(dbFactory);

        var profile = new CandidateProfile
        {
            Name = "Canon Taito",
            Headline = "Engineer",
            Location = "Springfield Lakes, QLD",
            Highlights = ["Original highlight."],
            Experience = [new ExperienceEntry { Company = "Acme", Title = "Developer" }],
        };
        var store = new FakeProfileStore(profile);
        var redactor = new PiiRedactor(profile, new InMemoryPiiMappingStore(), NullLogger<PiiRedactor>.Instance);
        var chat = new ScriptedChatClient(
            """{"resumeBullets":["Tailored bullet for [EMPLOYER_1]."],"coverLetter":"Dear [NAME], hello."}""");
        var agent = new TailoringAgent(chat, redactor, store, NullLoggerFactory.Instance);
        var service = new TailoringService(agent, redactor, store, dbFactory);

        var draft = await service.TailorAsync(applicationId, CancellationToken.None);

        Assert.NotNull(draft);
        Assert.Equal("Tailored bullet for Acme.", Assert.Single(draft!.ResumeBullets));
        Assert.Equal("Dear Canon Taito, hello.", draft.CoverLetter);

        await using var db = dbFactory.CreateDbContext();
        var stored = await db.Applications.FindAsync(applicationId);
        Assert.NotNull(stored);
        Assert.Equal(JobApplicationStatus.Tailored, stored!.Status);
        Assert.NotNull(stored.TailoredProfileJson);

        var tailored = JsonSerializer.Deserialize<CandidateProfile>(
            stored.TailoredProfileJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(tailored);
        Assert.Equal("Canon Taito", tailored!.Name);
        Assert.Equal("Tailored bullet for Acme.", Assert.Single(tailored.Highlights));
    }

    private static async Task<int> SeedApplicationAsync(TestDbContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var application = new JobApplication
        {
            SourceId = "job-1",
            Source = "fake",
            Title = "Engineer",
            Company = "Acme",
            Description = "An engineer role.",
            Status = JobApplicationStatus.Scored,
            DiscoveredAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();
        return application.Id;
    }
}
