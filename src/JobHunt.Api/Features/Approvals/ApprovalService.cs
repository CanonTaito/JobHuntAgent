using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Approvals;

public sealed class ApprovalService
{
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(IDbContextFactory<JobHuntDbContext> contexts, ILogger<ApprovalService> logger)
    {
        _contexts = contexts;
        _logger = logger;
    }

    public Task<ApplicationTransitionResult> RequestApprovalAsync(int applicationId, CancellationToken ct) =>
        TransitionAsync(applicationId, JobApplicationStatus.AwaitingApproval, ct);

    public Task<ApplicationTransitionResult> ApproveAsync(int applicationId, CancellationToken ct) =>
        TransitionAsync(applicationId, JobApplicationStatus.Applied, ct);

    public Task<ApplicationTransitionResult> RejectAsync(int applicationId, CancellationToken ct) =>
        TransitionAsync(applicationId, JobApplicationStatus.Rejected, ct);

    private async Task<ApplicationTransitionResult> TransitionAsync(
        int applicationId,
        JobApplicationStatus target,
        CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
        {
            return new ApplicationTransitionResult(TransitionOutcome.NotFound, null, null);
        }

        var current = application.Status;
        if (!ApplicationStatusTransitions.CanTransition(current, target))
        {
            return new ApplicationTransitionResult(
                TransitionOutcome.InvalidState,
                null,
                $"Cannot move application {applicationId} from {current} to {target}.");
        }

        application.Status = target;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Application {ApplicationId} transitioned from {From} to {To}",
            applicationId, current, target);

        return new ApplicationTransitionResult(TransitionOutcome.Succeeded, ApplicationView.From(application), null);
    }
}
