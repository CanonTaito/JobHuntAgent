using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Pii;
using Microsoft.EntityFrameworkCore;

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

        application.TailoredBullets = string.Join('\n', rehydrated.ResumeBullets);
        application.CoverLetter = rehydrated.CoverLetter;
        application.TailoredAt = DateTimeOffset.UtcNow;
        application.Status = JobApplicationStatus.Tailored;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return rehydrated;
    }
}
