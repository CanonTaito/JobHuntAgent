using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Observability;

namespace JobHunt.Api.Features.Salary;

public sealed class SalaryBandInference
{
    private const int ProbeLimit = 1000;

    private readonly IJobSource _source;

    public SalaryBandInference(IJobSource source) => _source = source;

    public async Task<SalaryBandResult> InferAsync(JobListing listing, JobSearchQuery context, CancellationToken ct)
    {
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("salary.infer");

        SalaryBandResult result;
        if (listing.SalaryMin is not null || listing.SalaryMax is not null)
        {
            var displayed = SalaryBand.Classify(listing.SalaryMin ?? listing.SalaryMax!.Value);
            result = new SalaryBandResult { Band = displayed, Source = SalarySource.Displayed };
        }
        else
        {
            result = await InferByProbingAsync(listing, context, ct);
        }

        activity?.SetTag("salary.source", result.Source.ToString());
        activity?.SetTag("salary.band", result.Band?.Label);
        return result;
    }

    public async Task<SalaryBandResult> InferByProbingAsync(JobListing listing, JobSearchQuery context, CancellationToken ct)
    {
        using var activity = JobHuntTelemetry.ActivitySource.StartActivity("salary.probe");

        foreach (var band in SalaryBand.All)
        {
            var probe = context with { SalaryMin = band.Min, SalaryMax = band.Max, MaxResults = ProbeLimit };
            if (await AppearsAsync(probe, listing, ct))
            {
                activity?.SetTag("salary.band", band.Label);
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
