using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Pii;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Features.Tailoring;

public sealed class TailoringService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly TailoringAgent _agent;
    private readonly PiiRedactor _redactor;
    private readonly IProfileStore _profiles;
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public TailoringService(
        TailoringAgent agent,
        PiiRedactor redactor,
        IProfileStore profiles,
        IDbContextFactory<JobHuntDbContext> contexts)
    {
        _agent = agent;
        _redactor = redactor;
        _profiles = profiles;
        _contexts = contexts;
    }

    public async Task<TailoringDraft?> TailorAsync(int applicationId, CancellationToken ct)
    {
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("tailoring.draft");
        activity?.SetTag("application.id", applicationId);

        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
        {
            return null;
        }

        var draft = await _agent.DraftAsync(application.Description, ct);

        var rehydrated = new TailoringDraft
        {
            ResumeBullets = draft.ResumeBullets.Select(_redactor.Rehydrate).ToList(),
            CoverLetter = _redactor.Rehydrate(draft.CoverLetter),
        };

        var tailoredProfile = BuildTailoredProfile(await _profiles.GetAsync(ct), rehydrated.ResumeBullets);

        application.TailoredBullets = string.Join('\n', rehydrated.ResumeBullets);
        application.CoverLetter = rehydrated.CoverLetter;
        application.TailoredProfileJson = JsonSerializer.Serialize(tailoredProfile, SerializerOptions);
        application.TailoredAt = DateTimeOffset.UtcNow;
        application.Status = JobApplicationStatus.Tailored;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        activity?.SetTag("tailoring.bullets", rehydrated.ResumeBullets.Count);
        return rehydrated;
    }

    private static CandidateProfile BuildTailoredProfile(CandidateProfile profile, IReadOnlyList<string> resumeBullets) =>
        resumeBullets.Count == 0
            ? profile
            : profile with { Highlights = resumeBullets };
}
