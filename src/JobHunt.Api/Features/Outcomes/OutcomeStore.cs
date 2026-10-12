using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Outcomes;

public sealed class OutcomeStore
{
    private static readonly JobApplicationStatus[] SubmittedStatuses =
        [JobApplicationStatus.Applied, JobApplicationStatus.Archived];

    private readonly IDbContextFactory<JobHuntDbContext> _contexts;
    private readonly ILogger<OutcomeStore> _logger;

    public OutcomeStore(IDbContextFactory<JobHuntDbContext> contexts, ILogger<OutcomeStore> logger)
    {
        _contexts = contexts;
        _logger = logger;
    }

    public async Task<OutcomeResult> RecordAsync(
        int applicationId,
        ApplicationOutcome outcome,
        string? note,
        CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
        {
            return new OutcomeResult(OutcomeResultStatus.NotFound, null, null);
        }

        if (!SubmittedStatuses.Contains(application.Status))
        {
            return new OutcomeResult(
                OutcomeResultStatus.NotSubmitted,
                null,
                $"Application {applicationId} has not been submitted (status {application.Status}); outcomes can only be recorded for Applied applications.");
        }

        var record = new OutcomeRecord
        {
            JobApplicationId = applicationId,
            Outcome = outcome,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            RecordedAt = DateTimeOffset.UtcNow,
        };

        db.Outcomes.Add(record);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recorded outcome {Outcome} for application {ApplicationId}",
            outcome, applicationId);

        return new OutcomeResult(OutcomeResultStatus.Succeeded, OutcomeView.From(record), null);
    }

    public async Task<IReadOnlyList<OutcomeView>?> ListAsync(int applicationId, CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();
        if (!await db.Applications.AnyAsync(a => a.Id == applicationId, ct))
        {
            return null;
        }

        var records = await db.Outcomes
            .AsNoTracking()
            .Where(o => o.JobApplicationId == applicationId)
            .ToListAsync(ct);

        return records
            .OrderByDescending(o => o.RecordedAt)
            .Select(OutcomeView.From)
            .ToList();
    }
}
