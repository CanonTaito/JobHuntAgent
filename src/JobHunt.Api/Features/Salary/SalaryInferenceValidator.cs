using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Features.Salary;

public sealed class SalaryInferenceValidator
{
    private readonly IJobSource _source;
    private readonly SalaryBandInference _inference;
    private readonly CandidateProfile _profile;

    public SalaryInferenceValidator(IJobSource source, SalaryBandInference inference, CandidateProfile profile)
    {
        _source = source;
        _inference = inference;
        _profile = profile;
    }

    public async Task<SalaryValidationReport> ValidateAsync(int sampleSize, CancellationToken ct)
    {
        var query = JobSearchQueryFactory.FromProfile(_profile, sampleSize);
        var listings = await _source.SearchAsync(query, ct);

        var checks = new List<SalaryValidationCheck>();
        var inferred = new List<SalaryValidationInference>();

        foreach (var listing in listings.Take(sampleSize))
        {
            var displayed = listing.SalaryMin ?? listing.SalaryMax;
            if (displayed is not null)
            {
                var displayedBand = SalaryBand.Classify(displayed.Value);
                var probed = await _inference.InferByProbingAsync(listing, query, ct);

                checks.Add(new SalaryValidationCheck
                {
                    SourceId = listing.Id,
                    Title = listing.Title,
                    DisplayedBand = displayedBand?.Label,
                    InferredBand = probed.Band?.Label,
                    Inferred = probed.Source == SalarySource.Inferred,
                    Matched = displayedBand is not null && probed.Band == displayedBand,
                });
            }
            else
            {
                var result = await _inference.InferAsync(listing, query, ct);
                inferred.Add(new SalaryValidationInference
                {
                    SourceId = listing.Id,
                    Title = listing.Title,
                    Band = result.Band?.Label,
                    Source = result.Source.ToString(),
                });
            }
        }

        var validated = checks.Count(check => check.InferredBand is not null);
        var accurate = checks.Count(check => check.Matched);

        return new SalaryValidationReport
        {
            Sampled = listings.Count,
            KnownSalary = checks.Count,
            Validated = validated,
            Accurate = accurate,
            Accuracy = validated == 0 ? null : (double)accurate / validated,
            Checks = checks,
            Inferred = inferred,
        };
    }
}
