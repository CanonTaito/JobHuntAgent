namespace JobHunt.Api.Features.Jobs;

public interface IJobSource
{
    string Name { get; }

    Task<IReadOnlyList<JobListing>> SearchAsync(JobSearchQuery query, CancellationToken ct);
}
