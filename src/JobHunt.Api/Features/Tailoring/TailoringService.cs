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
    private readonly TailoringAgent _agent;
    private readonly PiiRedactor _redactor;
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public TailoringService(
        TailoringAgent agent,
        PiiRedactor redactor,
        IDbContextFactory<JobHuntDbContext> contexts)
    {
        _agent = agent;
        _redactor = redactor;
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

        CandidateProfile? rehydratedProfile = null;
        if (draft.TailoredProfile is not null)
        {
            // The agent returns a redacted profile (placeholders). Rehydrate by
            // serializing to JSON, performing string-based rehydration, then
            // deserializing back into the CandidateProfile model.
            var json = JsonSerializer.Serialize(draft.TailoredProfile, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var rehydratedJson = _redactor.Rehydrate(json);
            rehydratedProfile = JsonSerializer.Deserialize<CandidateProfile>(rehydratedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        var rehydrated = new TailoringDraft
        {
            ResumeBullets = draft.ResumeBullets.Select(_redactor.Rehydrate).ToList(),
            CoverLetter = _redactor.Rehydrate(draft.CoverLetter),
            TailoredProfile = rehydratedProfile,
        };

        application.TailoredBullets = string.Join('\n', rehydrated.ResumeBullets);
        application.CoverLetter = rehydrated.CoverLetter;
        if (rehydratedProfile is not null)
        {
            application.TailoredProfileJson = JsonSerializer.Serialize(rehydratedProfile, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        // Note: we return the full tailored profile in the response (if present).
        application.TailoredAt = DateTimeOffset.UtcNow;
        application.Status = JobApplicationStatus.Tailored;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        activity?.SetTag("tailoring.bullets", rehydrated.ResumeBullets.Count);
        return rehydrated;
    }
}
