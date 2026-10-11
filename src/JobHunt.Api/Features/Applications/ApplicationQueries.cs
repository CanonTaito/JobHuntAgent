using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Applications;

public sealed class ApplicationQueries
{
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public ApplicationQueries(IDbContextFactory<JobHuntDbContext> contexts) => _contexts = contexts;

    public async Task<IReadOnlyList<ApplicationView>> ListAsync(JobApplicationStatus? status, CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();

        var query = db.Applications.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var applications = await query.ToListAsync(ct);

        return applications
            .OrderByDescending(a => a.OverallScore)
            .ThenByDescending(a => a.DiscoveredAt)
            .Select(ApplicationView.From)
            .ToList();
    }

    public async Task<ApplicationView?> GetAsync(int applicationId, CancellationToken ct)
    {
        await using var db = _contexts.CreateDbContext();
        var application = await db.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        return application is null ? null : ApplicationView.From(application);
    }
}
