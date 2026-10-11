using JobHunt.Api.Features.Jobs;

namespace JobHunt.Api.Features.Salary;

public sealed class SalaryBandInference
{
    private const int ProbeLimit = 1000;

    private readonly IJobSource _source;

    public SalaryBandInference(IJobSource source) => _source = source;

    public async Task<SalaryBandResult> InferAsync(JobListing listing, JobSearchQuery context, CancellationToken ct)
    {
        if (listing.SalaryMin is not null || listing.SalaryMax is not null)
        {
            var displayed = SalaryBand.Classify(listing.SalaryMin ?? listing.SalaryMax!.Value);
            return new SalaryBandResult { Band = displayed, Source = SalarySource.Displayed };
        }

        foreach (var band in SalaryBand.All)
        {
            var probe = context with { SalaryMin = band.Min, SalaryMax = band.Max, MaxResults = ProbeLimit };
            if (await AppearsAsync(probe, listing, ct))
            {
                return new SalaryBandResult { Band = band, Source = SalarySource.Inferred };
            }
        }

        return new SalaryBandResult { Source = SalarySource.Unknown };
    }

    private async Task<bool> AppearsAsync(JobSearchQuery query, JobListing target, CancellationToken ct)
    {
        var results = await _source.SearchAsync(query, ct);
        return results.Any(result => result.Source == target.Source && result.Id == target.Id);
    }
}
