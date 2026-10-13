using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Pii;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.FollowUps;

public sealed class FollowUpService
{
    private readonly FollowUpAgent _agent;
    private readonly PiiRedactor _redactor;
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public FollowUpService(
        FollowUpAgent agent,
        PiiRedactor redactor,
        IDbContextFactory<JobHuntDbContext> contexts)
    {
        _agent = agent;
        _redactor = redactor;
        _contexts = contexts;
    }

    public async Task<FollowUpResult> DraftAsync(int applicationId, CancellationToken ct)
    {
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("followup.draft");
        activity?.SetTag("application.id", applicationId);

        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
        {
            return new FollowUpResult(FollowUpResultStatus.NotFound, null, null);
        }

        if (application.Status != JobApplicationStatus.Applied)
        {
            return new FollowUpResult(
                FollowUpResultStatus.NotApplied,
                null,
                $"Application {applicationId} has not been applied (status {application.Status}); follow-ups can only be drafted for Applied applications.");
        }

        var draft = await _agent.DraftAsync(application.Description, ct);
        var rehydrated = new FollowUpDraft
        {
            Subject = _redactor.Rehydrate(draft.Subject),
            Body = _redactor.Rehydrate(draft.Body),
            SendAfterDays = draft.SendAfterDays,
        };

        application.FollowUpSubject = rehydrated.Subject;
        application.FollowUpBody = rehydrated.Body;
        application.FollowUpSendAfterDays = rehydrated.SendAfterDays;
        application.FollowUpDraftedAt = DateTimeOffset.UtcNow;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        activity?.SetTag("followup.send_after_days", rehydrated.SendAfterDays);
        return new FollowUpResult(FollowUpResultStatus.Succeeded, rehydrated, null);
    }
}
