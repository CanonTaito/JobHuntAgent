using JobHunt.Api.Features.Jobs;

namespace JobHunt.Api.Tests.TestSupport;

public sealed class FilteringJobSource : IJobSource
{
    private readonly IReadOnlyList<Seed> _seeds;

    public FilteringJobSource(IEnumerable<(JobListing Listing, int TrueMin, int TrueMax)> seeds) =>
        _seeds = seeds.Select(seed => new Seed(seed.Listing, seed.TrueMin, seed.TrueMax)).ToList();

    public string Name => "fake";

    public int SearchCount { get; private set; }

    public Task<IReadOnlyList<JobListing>> SearchAsync(JobSearchQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        SearchCount++;

        IEnumerable<Seed> matches = _seeds;

        if (query.Keywords.Count > 0)
        {
            matches = matches.Where(seed => query.Keywords.Any(keyword =>
                Contains(seed.Listing.Title, keyword) || Contains(seed.Listing.Description, keyword)));
        }

        if (query.Locations.Count > 0)
        {
            matches = matches.Where(seed => query.Locations.Any(location => Contains(seed.Listing.Location, location)));
        }

        if (query.WorkModels.Count > 0)
        {
            matches = matches.Where(seed => query.WorkModels.Any(model =>
                string.Equals(seed.Listing.WorkModel, model, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.SalaryMin is not null || query.SalaryMax is not null)
        {
            matches = matches.Where(seed =>
                seed.TrueMax >= (query.SalaryMin ?? 0) &&
                (query.SalaryMax is null || seed.TrueMin <= query.SalaryMax));
        }

        IReadOnlyList<JobListing> results = matches
            .Take(query.MaxResults > 0 ? query.MaxResults : int.MaxValue)
            .Select(seed => seed.Listing)
            .ToList();

        return Task.FromResult(results);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record Seed(JobListing Listing, int TrueMin, int TrueMax);
}
